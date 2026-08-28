using BFF.Models.Auth;

namespace BFF.Services.Authentication;

public sealed class UnconfiguredApplicationTokenRefreshClient : IApplicationTokenRefreshClient
{
    public Task<AuthenticationCallResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Application access-token refresh is not configured.");
}
