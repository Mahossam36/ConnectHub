using ConnectHub.BLL.Interfaces.Services;

namespace ConnectHub.BLL.Services;

public class NullRealTimeNotificationService : IRealTimeNotificationService
{
    public Task SendNotificationToUserAsync(Guid userId, object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendPostCreatedToGroupAsync(Guid groupId, object post, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendCommentCreatedToPostAsync(Guid postId, object comment, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
