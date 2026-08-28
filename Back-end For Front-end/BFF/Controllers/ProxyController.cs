using BFF.Services.Authentication;
using BFF.Services.Integration;
using BFF.Services.Sessions;
using BFF.Services.Moderation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BFF.Controllers;

[ApiController]
[Route("api/{**path}")]
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
        var session = await sessionService.GetCurrentAsync(HttpContext, cancellationToken);
        if (session is null)
        {
            sessionService.ClearCookie(HttpContext);
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

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
            await Response.WriteAsJsonAsync(new { code = "CONTENT_MODERATION_FAILED", message = "This content doesn’t meet our community guidelines. Please try something else." }, cancellationToken);
            return;
        }
        if (moderation == ModerationOutcome.Unavailable)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { code = "MODERATION_UNAVAILABLE", message = "We couldn’t check your content right now. Please try again in a moment." }, cancellationToken);
            return;
        }

        session = await accessTokenService.GetSessionWithValidAccessTokenAsync(session, cancellationToken);
        if (session is null)
        {
            await sessionService.RemoveCurrentAsync(HttpContext, cancellationToken);
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using var upstream = await integrationClient.ForwardAsync(Request, path, session, cancellationToken);
        Response.StatusCode = (int)upstream.StatusCode;
        CopyHeaders(upstream);
        await upstream.Content.CopyToAsync(Response.Body, cancellationToken);
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
