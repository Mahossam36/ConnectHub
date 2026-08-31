using BFF.Configuration;
using BFF.Models.Sessions;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace BFF.Services.Integration;

public sealed class IntegrationClient(
    IHttpClientFactory httpClientFactory,
    IIntegrationTokenService integrationTokenService,
    IOptions<IntegrationOptions> options,
    ILogger<IntegrationClient> logger) : IIntegrationClient
{
    private static readonly HashSet<string> ForwardedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept", "Accept-Language", "If-Match", "If-None-Match", "Range"
    };

    private static readonly HashSet<string> MethodsWithBody = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH"
    };

    private readonly IntegrationOptions _options = options.Value;

    public async Task<HttpResponseMessage> ForwardAsync(HttpRequest request, string? path, UserSession? session, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.ConnectHubPath))
            throw new InvalidOperationException("ConnectHub integration configuration is not configured.");
        var safePath = ValidatePath(path);
        var targetPath = safePath.StartsWith("uploads", StringComparison.OrdinalIgnoreCase) ? safePath : $"api/{safePath}";
        var targetUri = BuildUri(targetPath, request.QueryString);
        var message = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);

        // Only attach a request body for methods that carry one (POST, PUT, PATCH).
        // GET / DELETE / HEAD requests must NOT include a body – some servers reject it.
        if (MethodsWithBody.Contains(request.Method))
        {
            // Rewind the incoming stream so we read from the beginning.
            // EnableBuffering() has been called in ProxyController making the stream seekable.
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }

            var memory = new MemoryStream();
            await request.Body.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;

            logger.LogInformation(
                "Forwarding {Method} {Path} — body size {Bytes} bytes, Content-Type: {ContentType}",
                request.Method, safePath, memory.Length, request.ContentType ?? "(none)");

            message.Content = new StreamContent(memory);

            // Preserve the original Content-Type (including multipart boundaries).
            if (!string.IsNullOrWhiteSpace(request.ContentType))
            {
                // Remove any default Content-Type first, then set the correct one.
                message.Content.Headers.Remove("Content-Type");
                message.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
            }

            // Use the actual byte count from the buffer, not the original Content-Length
            // which may be stale if the stream was partially read upstream.
            message.Content.Headers.ContentLength = memory.Length;
        }

        foreach (var header in request.Headers.Where(header => ForwardedHeaders.Contains(header.Key)))
            message.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());

        var integrationToken =
       await integrationTokenService.GetValidAsync(cancellationToken);

        message.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", integrationToken);

        if (!string.IsNullOrWhiteSpace(session?.AccessToken))
        {
            message.Headers.TryAddWithoutValidation(
                "Access-Token",
                session.AccessToken);
        }

        var response = await httpClientFactory.CreateClient("IntegrationLayer")
            .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        logger.LogInformation(
            "Upstream responded {StatusCode} for {Method} {Path}",
            (int)response.StatusCode, request.Method, safePath);

        return response;
    }

    private Uri BuildUri(string path, QueryString queryString) => new(
        $"{_options.BaseUrl.TrimEnd('/')}/{_options.ConnectHubPath.Trim('/')}/{path}{queryString}",
        UriKind.Absolute);

    private static string ValidatePath(string? path)
    {
        var normalized = (path ?? string.Empty).Trim('/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains("://", StringComparison.Ordinal) ||
            normalized.Split('/').Any(segment => segment is "." or ".."))
            throw new BadHttpRequestException("Invalid integration path.", StatusCodes.Status400BadRequest);
        return normalized;
    }
}
