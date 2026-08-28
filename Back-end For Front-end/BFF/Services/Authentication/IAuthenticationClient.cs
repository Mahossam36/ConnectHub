using BFF.Models.Auth;

namespace BFF.Services.Authentication;

public interface IAuthenticationClient
{
    Task<AuthenticationCallResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationCallResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationCallResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationCallResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthenticationOperationResult> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthenticationOperationResult> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public sealed record AuthenticationOperationResult(int StatusCode, string? ErrorBody)
{
    public bool Succeeded => StatusCode is >= 200 and <= 299;
}

public sealed record AuthenticationCallResult(int StatusCode, UpstreamAuthResponse? Authentication, string? ErrorBody)
{
    public bool Succeeded => StatusCode is >= 200 and <= 299 && Authentication is not null;
}
