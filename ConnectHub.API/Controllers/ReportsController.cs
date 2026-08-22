using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Reports;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Submit a moderation report against a post or comment.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportResponseDto>> SubmitReport([FromBody] CreateReportRequestDto request, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _reportService.SubmitReportAsync(currentUserId, request, cancellationToken);
        return ToCreatedResult(result);
    }

    /// <summary>
    /// Get paginated list of moderation reports.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ReportResponseDto>>> GetReports(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetRequiredUserId();
        var pagination = new PaginationParams { Skip = skip, Take = take };
        var result = await _reportService.GetReportsAsync(currentUserId, pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Resolve a moderation report (ActionTaken or Dismissed).
    /// </summary>
    [HttpPut("{id:guid}/resolve")]
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportResponseDto>> ResolveReport(
        Guid id,
        [FromBody] ResolveReportRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _reportService.ResolveReportAsync(id, currentUserId, request, cancellationToken);
        return ToActionResult(result);
    }
}
