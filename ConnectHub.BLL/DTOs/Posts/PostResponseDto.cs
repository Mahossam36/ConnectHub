using ConnectHub.BLL.DTOs.Attachments;
using ConnectHub.BLL.DTOs.Users;

namespace ConnectHub.BLL.DTOs.Posts;

public class PostResponseDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public UserSummaryDto Author { get; set; } = null!;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public List<AttachmentResponseDto> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
