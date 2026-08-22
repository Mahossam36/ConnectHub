using ConnectHub.Models.Enums;

namespace ConnectHub.BLL.DTOs.Reports;

public class ResolveReportRequestDto
{
    public ReportStatus Status { get; set; } // ActionTaken or Dismissed
}
