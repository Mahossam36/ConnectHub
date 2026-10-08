namespace ConnectHub.BLL.DTOs.Comments;

public class CreateCommentRequestDto
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}
