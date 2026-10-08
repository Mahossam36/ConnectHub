using Ardalis.Result;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Notifications;
using ConnectHub.Models.Enums;

namespace ConnectHub.BLL.Interfaces.Services;

public interface INotificationService
{
    Task<Result<NotificationFeedResponseDto>> GetNotificationsAsync(Guid currentUserId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Result> MarkAsReadAsync(Guid notificationId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> MarkAllAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> DeleteNotificationAsync(Guid notificationId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> DispatchNotificationAsync(Guid recipientUserId, NotificationType type, string message, string? targetUrl = null, CancellationToken cancellationToken = default);
}
