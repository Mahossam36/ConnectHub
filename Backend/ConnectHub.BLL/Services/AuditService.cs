using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class AuditService : IAuditService
{
    private readonly IGenericRepository<AuditLog> _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IGenericRepository<AuditLog> auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<AuditService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        Guid? userId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Timestamp = DateTime.UtcNow,
            Metadata = metadata
        };

        await _auditLogRepository.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Audit: Action {Action} on {EntityType}:{EntityId} by User {UserId}", action, entityType, entityId, userId);
    }
}
