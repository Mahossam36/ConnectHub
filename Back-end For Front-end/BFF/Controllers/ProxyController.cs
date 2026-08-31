using BFF.Services.Authentication;
using BFF.Services.Integration;
using BFF.Services.Sessions;
using BFF.Services.Moderation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BFF.Controllers;

[ApiController]
[Route("api/{**path}")]
[Route("uploads/{**path}")]
public sealed class ProxyController(
    ISessionService sessionService,
    IAccessTokenService accessTokenService,
    IIntegrationClient integrationClient,
    IContentModerationService moderationService,
    ILogger<ProxyController> logger) : ControllerBase
{
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    public async Task Proxy(string? path, CancellationToken cancellationToken)
    {
        // Ensure request body can be read multiple times by downstream components
        // (e.g. moderation) and by the integration forwarder.
        Request.EnableBuffering();

        // DEBUG: Log body state right after EnableBuffering
        logger.LogInformation(
            "[BODY-TRACE] After EnableBuffering — Method={Method}, Path={Path}, BodyType={BodyType}, CanSeek={CanSeek}, CanRead={CanRead}, Position={Position}, ContentLength={ContentLength}, ContentType={ContentType}",
            Request.Method, path,
            Request.Body.GetType().Name,
            Request.Body.CanSeek,
            Request.Body.CanRead,
            Request.Body.CanSeek ? Request.Body.Position : -1,
            Request.ContentLength,
            Request.ContentType);

        var isUploadsPath = Request.Path.StartsWithSegments("/uploads");
        var isPublicAttachmentDownload = Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && path?.StartsWith("Attachments/", StringComparison.OrdinalIgnoreCase) == true;
        var allowsAnonymousMedia = isUploadsPath || isPublicAttachmentDownload;
        var session = await sessionService.GetCurrentAsync(HttpContext, cancellationToken);
        if (session is null && !allowsAnonymousMedia)
        {
            sessionService.ClearCookie(HttpContext);
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // DEBUG: Log body state after session retrieval
        logger.LogInformation(
            "[BODY-TRACE] After GetCurrentAsync — Position={Position}, Length={Length}",
            Request.Body.CanSeek ? Request.Body.Position : -1,
            Request.Body.CanSeek ? Request.Body.Length : -1);

        var moderation = await ModerateIfRequiredAsync(path, cancellationToken);
        if (moderation == ModerationOutcome.Invalid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { code = "INVALID_CONTENT", message = "Content is required." }, cancellationToken);
            return;
        }
        if (moderation == ModerationOutcome.Flagged)
        {
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await Response.WriteAsJsonAsync(new { code = "CONTENT_MODERATION_FAILED", message = "This content doesn't meet our community guidelines. Please try something else." }, cancellationToken);
            return;
        }
        if (moderation == ModerationOutcome.Unavailable)
        {
            logger.LogWarning("Content moderation service was unavailable; proceeding with request.");
        }

        // DEBUG: Log body state after moderation
        logger.LogInformation(
            "[BODY-TRACE] After Moderation — Position={Position}, Length={Length}",
            Request.Body.CanSeek ? Request.Body.Position : -1,
            Request.Body.CanSeek ? Request.Body.Length : -1);

        if (session is not null)
        {
            session = await accessTokenService.GetSessionWithValidAccessTokenAsync(session, cancellationToken);
            if (session is null && !allowsAnonymousMedia)
            {
                await sessionService.RemoveCurrentAsync(HttpContext, cancellationToken);
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        // DEBUG: Log body state before forwarding
        logger.LogInformation(
            "[BODY-TRACE] Before ForwardAsync — Position={Position}, Length={Length}",
            Request.Body.CanSeek ? Request.Body.Position : -1,
            Request.Body.CanSeek ? Request.Body.Length : -1);

        try
        {
            var effectivePath = Request.Path.StartsWithSegments("/uploads") ? Request.Path.Value?.TrimStart('/') : path;
            using var upstream = await integrationClient.ForwardAsync(Request, effectivePath ?? path, session, cancellationToken);
            Response.StatusCode = (int)upstream.StatusCode;
            CopyHeaders(upstream);
            await upstream.Content.CopyToAsync(Response.Body, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer debounced client request superseded this one, or the connection closed; no response is required.
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            logger.LogWarning(ex, "Upstream connection aborted for /api/{Path}", path);
            if (!Response.HasStarted)
            {
                Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
    }

    private async Task<ModerationOutcome> ModerateIfRequiredAsync(string? path, CancellationToken cancellationToken)
    {
        if (!RequiresModeration(Request.Method, path)) return ModerationOutcome.NotRequired;

        Request.EnableBuffering();
        try
        {
            using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
            Request.Body.Position = 0;
            if (!document.RootElement.TryGetProperty("content", out var contentProperty) || contentProperty.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(contentProperty.GetString()))
                return ModerationOutcome.Invalid;

            logger.LogInformation("Content moderation started for {Operation}.", $"{Request.Method} /api/{path}");
            var result = await moderationService.ModerateAsync(contentProperty.GetString()!, cancellationToken);
            return result.IsUnavailable ? ModerationOutcome.Unavailable : result.IsAllowed ? ModerationOutcome.Allowed : ModerationOutcome.Flagged;
        }
        catch (JsonException)
        {
            Request.Body.Position = 0;
            return ModerationOutcome.Invalid;
        }
    }

    private static bool RequiresModeration(string method, string? path)
    {
        var segments = (path ?? string.Empty).Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            return segments.Length == 5 && segments[0].Equals("Posts", StringComparison.OrdinalIgnoreCase) && segments[1].Equals("api", StringComparison.OrdinalIgnoreCase) &&
                   segments[2].Equals("groups", StringComparison.OrdinalIgnoreCase) && segments[4].Equals("posts", StringComparison.OrdinalIgnoreCase)
                || segments.Length == 5 && segments[0].Equals("Comments", StringComparison.OrdinalIgnoreCase) && segments[1].Equals("api", StringComparison.OrdinalIgnoreCase) &&
                   segments[2].Equals("posts", StringComparison.OrdinalIgnoreCase) && segments[4].Equals("comments", StringComparison.OrdinalIgnoreCase);
        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            return segments.Length == 4 && segments[1].Equals("api", StringComparison.OrdinalIgnoreCase) &&
                   ((segments[0].Equals("Posts", StringComparison.OrdinalIgnoreCase) && segments[2].Equals("posts", StringComparison.OrdinalIgnoreCase)) ||
                    (segments[0].Equals("Comments", StringComparison.OrdinalIgnoreCase) && segments[2].Equals("comments", StringComparison.OrdinalIgnoreCase)));
        return false;
    }

    private enum ModerationOutcome { NotRequired, Allowed, Flagged, Unavailable, Invalid }

    private void CopyHeaders(HttpResponseMessage upstream)
    {
        foreach (var header in upstream.Headers.Concat(upstream.Content.Headers))
        {
            if (header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
            Response.Headers[header.Key] = header.Value.ToArray();
        }
    }
}
