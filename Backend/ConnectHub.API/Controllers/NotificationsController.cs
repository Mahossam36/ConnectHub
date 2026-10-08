using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Notifications;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get paginated notifications feed and unread count for current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationFeedResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationFeedResponseDto>> GetNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetRequiredUserId();
        var pagination = new PaginationParams { Skip = skip, Take = take };
        var result = await _notificationService.GetNotificationsAsync(currentUserId, pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Mark a notification as read.
    /// </summary>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _notificationService.MarkAsReadAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Mark all notifications as read for current user.
    /// </summary>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _notificationService.MarkAllAsReadAsync(currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Delete a notification.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _notificationService.DeleteNotificationAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }
}
