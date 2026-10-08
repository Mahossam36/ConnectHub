using ConnectHub.BLL.DTOs.Users;
using ConnectHub.Models.Enums;

namespace ConnectHub.BLL.DTOs.Reports;

public class ReportResponseDto
{
    public Guid Id { get; set; }
    public UserSummaryDto ReportedBy { get; set; } = null!;
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
