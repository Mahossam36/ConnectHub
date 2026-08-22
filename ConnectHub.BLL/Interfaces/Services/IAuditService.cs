namespace ConnectHub.BLL.Interfaces.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId = null, Guid? userId = null, string? metadata = null, CancellationToken cancellationToken = default);
}
