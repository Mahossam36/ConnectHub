using ConnectHub.Models.Enums;

namespace ConnectHub.Models.Entities;

/// <summary>Moderation report for a post or comment.</summary>
public class Report
{
    public Guid Id { get; set; }

    /// <summary>Reporter User FK.</summary>
    public Guid ReportedById { get; set; }

    public ReportTargetType TargetType { get; set; }

    /// <summary>Reported content ID (Post or Comment).</summary>
    public Guid TargetId { get; set; }

    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public User ReportedBy { get; set; } = null!;
}
