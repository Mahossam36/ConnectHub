using BFF.Models.Auth;

namespace BFF.Services.Authentication;

public sealed class IntegrationApplicationTokenRefreshClient(IAuthenticationClient authenticationClient) : IApplicationTokenRefreshClient
{
    public Task<AuthenticationCallResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        authenticationClient.RefreshAsync(refreshToken, cancellationToken);
}
