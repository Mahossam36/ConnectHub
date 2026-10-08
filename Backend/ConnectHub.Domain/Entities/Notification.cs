using ConnectHub.Models.Enums;

namespace ConnectHub.Models.Entities;

/// <summary>In-app user notification.</summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>The target user.</summary>
    public User User { get; set; } = null!;
}
