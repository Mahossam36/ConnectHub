using Ardalis.Result;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Reports;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IReportService
{
    Task<Result<ReportResponseDto>> SubmitReportAsync(Guid currentUserId, CreateReportRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<PagedResultDto<ReportResponseDto>>> GetReportsAsync(Guid currentUserId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Result<ReportResponseDto>> ResolveReportAsync(Guid reportId, Guid currentUserId, ResolveReportRequestDto request, CancellationToken cancellationToken = default);
}
