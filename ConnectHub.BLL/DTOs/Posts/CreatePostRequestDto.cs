namespace ConnectHub.BLL.DTOs.Posts;

public class CreatePostRequestDto
{
    public string Content { get; set; } = string.Empty;
    public List<Guid> AttachmentIds { get; set; } = new();
}
