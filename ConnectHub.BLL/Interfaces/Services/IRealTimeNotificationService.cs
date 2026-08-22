namespace ConnectHub.BLL.Interfaces.Services;

/// <summary>
/// Abstraction for broadcasting real-time events and notifications across clients (via SignalR in API layer).
/// </summary>
public interface IRealTimeNotificationService
{
    Task SendNotificationToUserAsync(Guid userId, object notification, CancellationToken cancellationToken = default);
    Task SendPostCreatedToGroupAsync(Guid groupId, object post, CancellationToken cancellationToken = default);
    Task SendCommentCreatedToPostAsync(Guid postId, object comment, CancellationToken cancellationToken = default);
}
