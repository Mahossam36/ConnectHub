using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Notifications;
using ConnectHub.Models.Enums;

namespace ConnectHub.BLL.Interfaces.Services;

public interface INotificationService
{
    Task<NotificationFeedResponseDto> GetNotificationsAsync(Guid currentUserId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task DeleteNotificationAsync(Guid notificationId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task DispatchNotificationAsync(Guid recipientUserId, NotificationType type, string message, string? targetUrl = null, CancellationToken cancellationToken = default);
}
