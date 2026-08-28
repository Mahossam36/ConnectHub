using BFF.Configuration;
using BFF.Models.Sessions;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace BFF.Services.Integration;

public sealed class IntegrationClient(
    IHttpClientFactory httpClientFactory,
    IIntegrationTokenService integrationTokenService,
    IOptions<IntegrationOptions> options) : IIntegrationClient
{
    private static readonly HashSet<string> ForwardedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept", "Accept-Language", "If-Match", "If-None-Match", "Range"
    };
    private readonly IntegrationOptions _options = options.Value;

    public async Task<HttpResponseMessage> ForwardAsync(HttpRequest request, string? path, UserSession session, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.ConnectHubPath))
            throw new InvalidOperationException("ConnectHub integration configuration is not configured.");
        var safePath = ValidatePath(path);
        using var message = new HttpRequestMessage(new HttpMethod(request.Method), BuildUri($"api/{safePath}", request.QueryString));
        if (request.ContentLength is > 0)
        {
            message.Content = new StreamContent(request.Body);
            if (!string.IsNullOrWhiteSpace(request.ContentType)) message.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
        }
        foreach (var header in request.Headers.Where(header => ForwardedHeaders.Contains(header.Key)))
            message.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());

        var integrationToken =
       await integrationTokenService.GetValidAsync(cancellationToken);

        message.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", integrationToken);

        message.Headers.TryAddWithoutValidation(
            "Access-Token",
            session.AccessToken);
        return await httpClientFactory.CreateClient("IntegrationLayer").SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
