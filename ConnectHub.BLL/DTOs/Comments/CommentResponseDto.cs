using ConnectHub.BLL.DTOs.Users;

namespace ConnectHub.BLL.DTOs.Comments;

public class CommentResponseDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public UserSummaryDto Author { get; set; } = null!;
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public List<CommentResponseDto> Replies { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
