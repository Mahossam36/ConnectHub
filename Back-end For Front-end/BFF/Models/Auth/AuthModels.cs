namespace BFF.Models.Auth;

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string FirstName, string LastName, string Email, string Password);
public sealed record ExternalLoginRequest(
    string Provider,
    string ProviderId,
    string Email,
    string FirstName,
    string LastName,
    string? ProfileImageUrl);
public sealed record RefreshRequest(string RefreshToken);

public sealed record UpstreamAuthResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);

public sealed record AuthenticatedUserResponse(Guid UserId, string Email, string DisplayName, string? AvatarUrl, DateTimeOffset ExpiresAt);
