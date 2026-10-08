using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Reports;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly IPostRepository _postRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository reportRepository,
        IPostRepository postRepository,
        ICommentRepository commentRepository,
        IGenericRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditService auditService,
        ILogger<ReportService> logger)
    {
        _reportRepository = reportRepository;
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Result<ReportResponseDto>> SubmitReportAsync(
        Guid currentUserId,
        CreateReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.TargetType == ReportTargetType.Post)
        {
            var postExists = await _postRepository.ExistsAsync(p => p.Id == request.TargetId);
            if (!postExists)
                return Result.NotFound($"Target post with ID '{request.TargetId}' was not found.");
        }
        else if (request.TargetType == ReportTargetType.Comment)
        {
            var commentExists = await _commentRepository.ExistsAsync(c => c.Id == request.TargetId);
            if (!commentExists)
                return Result.NotFound($"Target comment with ID '{request.TargetId}' was not found.");
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReportedById = currentUserId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            Reason = request.Reason.Trim(),
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _reportRepository.AddAsync(report);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("SubmitReport", "Report", report.Id, currentUserId, $"TargetType:{request.TargetType},TargetId:{request.TargetId}", cancellationToken);
        _logger.LogInformation("Report {ReportId} submitted by user {UserId} against {TargetType} {TargetId}.", report.Id, currentUserId, request.TargetType, request.TargetId);

        var detailedReport = await _reportRepository.GetWithDetailsAsync(report.Id);
        var dto = _mapper.Map<ReportResponseDto>(detailedReport ?? report);

        return Result.Success(dto);
    }

    public async Task<Result<PagedResultDto<ReportResponseDto>>> GetReportsAsync(
        Guid currentUserId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _reportRepository.Query();

        var total = await query.CountAsync(cancellationToken);

        var reports = await query
            .Include(r => r.ReportedBy)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<ReportResponseDto>>(reports);

        return Result.Success(new PagedResultDto<ReportResponseDto>
        {
            Items = dtos,
            Total = total,
            Skip = pagination.Skip,
            Take = pagination.Take
        });
    }

    public async Task<Result<ReportResponseDto>> ResolveReportAsync(
        Guid reportId,
        Guid currentUserId,
        ResolveReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetWithDetailsAsync(reportId);
        if (report is null)
            return Result.NotFound($"Report with ID '{reportId}' was not found.");

        report.Status = request.Status;
        report.ReviewedAt = DateTime.UtcNow;

        _reportRepository.Update(report);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("ResolveReport", "Report", reportId, currentUserId, $"NewStatus:{request.Status}", cancellationToken);
        _logger.LogInformation("Report {ReportId} resolved with status {Status} by user {UserId}.", reportId, request.Status, currentUserId);

        var dto = _mapper.Map<ReportResponseDto>(report);
        return Result.Success(dto);
    }
}
