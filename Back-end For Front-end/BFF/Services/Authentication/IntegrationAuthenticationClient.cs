using System.Net.Http.Json;
using System.Net.Http.Headers;
using BFF.Configuration;
using BFF.Models.Auth;
using BFF.Services.Integration;
using Microsoft.Extensions.Options;

namespace BFF.Services.Authentication;

public sealed class IntegrationAuthenticationClient(
    IHttpClientFactory httpClientFactory,
    IOptions<IntegrationOptions> integrationOptions,
    IOptions<AuthenticationOptions> authenticationOptions,
    IIntegrationTokenService integrationTokenService,
    ILogger<IntegrationAuthenticationClient> logger) : IAuthenticationClient
{
    private readonly IntegrationOptions _integration = integrationOptions.Value;
    private readonly AuthenticationOptions _authentication = authenticationOptions.Value;

    public Task<AuthenticationCallResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(_authentication.LoginPath, request, cancellationToken);

    public Task<AuthenticationCallResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(_authentication.RegisterPath, request, cancellationToken);

    public Task<AuthenticationCallResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(_authentication.ExternalLoginPath, request, cancellationToken);

    public Task<AuthenticationCallResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        SendAsync(_authentication.RefreshPath, new RefreshRequest(refreshToken), cancellationToken);

    public Task<AuthenticationOperationResult> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        SendOperationAsync(_authentication.RevokePath, new RefreshRequest(refreshToken), cancellationToken);

    public Task<AuthenticationOperationResult> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        SendOperationAsync(_authentication.LogoutPath, new RefreshRequest(refreshToken), cancellationToken);

    private async Task<AuthenticationCallResult> SendAsync(string path, object request, CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(path);
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(request) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await integrationTokenService.GetValidAsync(cancellationToken));
        using var response = await httpClientFactory.CreateClient("IntegrationLayer").SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new AuthenticationCallResult((int)response.StatusCode, null, await response.Content.ReadAsStringAsync(cancellationToken));

        var authentication = await response.Content.ReadFromJsonAsync<UpstreamAuthResponse>(cancellationToken: cancellationToken);
        if (authentication is null || string.IsNullOrWhiteSpace(authentication.AccessToken) || string.IsNullOrWhiteSpace(authentication.RefreshToken))
        {
            logger.LogError("The configured authentication endpoint returned an incomplete authentication response.");
            return new AuthenticationCallResult(StatusCodes.Status502BadGateway, null, "Authentication service returned an invalid response.");
        }

        return new AuthenticationCallResult((int)response.StatusCode, authentication, null);
    }

    private async Task<AuthenticationOperationResult> SendOperationAsync(string path, object request, CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(path);
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(request) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await integrationTokenService.GetValidAsync(cancellationToken));
        using var response = await httpClientFactory.CreateClient("IntegrationLayer").SendAsync(message, cancellationToken);
        return new AuthenticationOperationResult((int)response.StatusCode,
            response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private Uri BuildEndpoint(string path)
    {
        if (string.IsNullOrWhiteSpace(_integration.BaseUrl) || string.IsNullOrWhiteSpace(_integration.ConnectHubPath) ||
            string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("Authentication integration is not configured.");

        return new Uri($"{_integration.BaseUrl.TrimEnd('/')}/{_integration.ConnectHubPath.Trim('/')}/api/{path.TrimStart('/')}", UriKind.Absolute);
    }
}
