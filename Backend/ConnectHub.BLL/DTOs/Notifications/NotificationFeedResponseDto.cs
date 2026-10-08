namespace ConnectHub.BLL.DTOs.Notifications;

public class NotificationFeedResponseDto
{
    public IReadOnlyList<NotificationResponseDto> Items { get; set; } = new List<NotificationResponseDto>();
    public int UnreadCount { get; set; }
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}
