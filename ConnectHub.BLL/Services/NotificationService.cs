using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Notifications;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IRealTimeNotificationService _realTimeNotification;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IRealTimeNotificationService realTimeNotification,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _realTimeNotification = realTimeNotification;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<NotificationFeedResponseDto>> GetNotificationsAsync(
        Guid currentUserId,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _notificationRepository.Query()
            .Where(n => n.UserId == currentUserId);

        var total = await query.CountAsync(cancellationToken);
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(currentUserId);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<NotificationResponseDto>>(notifications);

        return Result.Success(new NotificationFeedResponseDto
        {
            Items = dtos,
            UnreadCount = unreadCount,
            Total = total,
            Skip = pagination.Skip,
            Take = pagination.Take
        });
    }

    public async Task<Result> MarkAsReadAsync(
        Guid notificationId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification is null)
            return Result.NotFound($"Notification with ID '{notificationId}' was not found.");

        if (notification.UserId != currentUserId)
            return Result.Forbidden("You do not have permission to modify this notification.");

        notification.IsRead = true;
        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}.", notificationId, currentUserId);
        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _notificationRepository.MarkAllAsReadAsync(currentUserId);
        _logger.LogInformation("All notifications marked as read for user {UserId}.", currentUserId);
        return Result.Success();
    }

    public async Task<Result> DeleteNotificationAsync(
        Guid notificationId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification is null)
            return Result.NotFound($"Notification with ID '{notificationId}' was not found.");

        if (notification.UserId != currentUserId)
            return Result.Forbidden("You do not have permission to delete this notification.");

        _notificationRepository.Delete(notification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Notification {NotificationId} deleted by user {UserId}.", notificationId, currentUserId);
        return Result.Success();
    }

    public async Task<Result> DispatchNotificationAsync(
        Guid recipientUserId,
        NotificationType type,
        string message,
        string? targetUrl = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = recipientUserId,
            Type = type,
            Message = message,
            TargetUrl = targetUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Notification dispatched to user {RecipientUserId} of type {Type}.", recipientUserId, type);

        var dto = _mapper.Map<NotificationResponseDto>(notification);

        // Real-time SignalR push to specific recipient user
        await _realTimeNotification.SendNotificationToUserAsync(recipientUserId, dto, cancellationToken);

        return Result.Success();
    }
}
