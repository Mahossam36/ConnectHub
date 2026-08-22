namespace ConnectHub.Models.Enums;

/// <summary>
/// Defines the type of event that triggered a notification.
/// Used to determine how the notification is rendered and what action the <c>TargetUrl</c> points to.
/// </summary>
public enum NotificationType
{
    NewPost = 1,
    NewComment = 2,
    NewReply = 3,
    PostLiked = 4,
    CommentLiked = 5,
    MemberJoined = 6,
    RoleChanged = 7,
}
