using ConnectHub.API.Hubs;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ConnectHub.API.Services;

public class RealTimeNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IHubContext<GroupHub> _groupHub;
    private readonly ILogger<RealTimeNotificationService> _logger;

    public RealTimeNotificationService(
        IHubContext<NotificationHub> notificationHub,
        IHubContext<GroupHub> groupHub,
        ILogger<RealTimeNotificationService> logger)
    {
        _notificationHub = notificationHub;
        _groupHub = groupHub;
        _logger = logger;
    }

    public async Task SendNotificationToUserAsync(Guid userId, object notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationHub.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast real-time notification to user {UserId}.", userId);
        }
    }

    public async Task SendPostCreatedToGroupAsync(Guid groupId, object post, CancellationToken cancellationToken = default)
    {
        try
        {
            await _groupHub.Clients.Group($"group_{groupId}").SendAsync("PostCreated", post, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast PostCreated event to group room {GroupId}.", groupId);
        }
    }

    public async Task SendCommentCreatedToPostAsync(Guid postId, object comment, CancellationToken cancellationToken = default)
    {
        try
        {
            await _groupHub.Clients.Group($"post_{postId}").SendAsync("CommentCreated", comment, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast CommentCreated event to post room {PostId}.", postId);
        }
    }
}
