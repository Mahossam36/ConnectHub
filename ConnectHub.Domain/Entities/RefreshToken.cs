namespace ConnectHub.Models.Entities;

/// <summary>
/// Persisted refresh token used for access-token rotation.
/// The raw token value is never stored; only its SHA-256 hash.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 hash of the raw token string. Never store the raw token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>User FK — same Guid as ApplicationUser.Id.</summary>
    public Guid UserId { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Set when the token is revoked (logout, rotation, or reuse detection).</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Human-readable reason for revocation (optional).</summary>
    public string? RevokedReason { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>The domain user who owns this token.</summary>
    public User User { get; set; } = null!;
}
