using ConnectHub.Models.Enums;

namespace ConnectHub.BLL.DTOs.Reports;

public class CreateReportRequestDto
{
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
