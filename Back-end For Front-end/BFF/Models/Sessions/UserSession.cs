namespace BFF.Models.Sessions;

public sealed record UserSession(
    string SessionId,
    Guid UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RefreshTokenExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset SessionExpiresAt);
