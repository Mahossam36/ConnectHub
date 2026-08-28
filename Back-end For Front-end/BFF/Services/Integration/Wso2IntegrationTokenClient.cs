using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BFF.Configuration;
using Microsoft.Extensions.Options;

namespace BFF.Services.Integration;

public sealed class Wso2IntegrationTokenClient(
    IHttpClientFactory httpClientFactory,
    IOptions<IntegrationOptions> options) : IIntegrationTokenClient
{
    private readonly IntegrationOptions _options = options.Value;

    public async Task<IntegrationTokenResult> AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.TokenPath) ||
            string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("WSO2 integration token configuration is incomplete.");

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{_options.BaseUrl.TrimEnd('/')}/{_options.TokenPath.TrimStart('/')}", UriKind.Absolute))
        {
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "client_credentials")]),
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"))) }
        };
        using var response = await httpClientFactory.CreateClient("IntegrationLayer").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("WSO2 token request failed.", null, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<Wso2TokenResponse>(cancellationToken: cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            throw new InvalidOperationException("WSO2 returned an invalid token response.");
        return new IntegrationTokenResult(payload.AccessToken, payload.ExpiresIn);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.TokenRevokePath) ||
            string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("WSO2 integration token revocation configuration is incomplete.");

        using var request = new HttpRequestMessage(HttpMethod.Post,
            new Uri($"{_options.BaseUrl.TrimEnd('/')}/{_options.TokenRevokePath.TrimStart('/')}", UriKind.Absolute))
        {
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("token", token)]),
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"))) }
        };
        using var response = await httpClientFactory.CreateClient("IntegrationLayer").SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("WSO2 token revocation request failed.", null, response.StatusCode);
    }

    private sealed record Wso2TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn);
}
