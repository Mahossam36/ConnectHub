using BFF.Models.Auth;

namespace BFF.Services.Authentication;

/// <summary>External application refresh boundary. Its HTTP contract is intentionally not assumed by the BFF.</summary>
public interface IApplicationTokenRefreshClient
{
    Task<AuthenticationCallResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}
