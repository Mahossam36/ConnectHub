namespace ConnectHub.Models.Enums;

/// <summary>
/// Represents the moderation lifecycle state of a submitted <see cref="ConnectHub.Models.Entities.Report"/>.
/// </summary>
public enum ReportStatus
{
    Pending = 1,
    ActionTaken = 2,
    Dismissed = 3,
}
