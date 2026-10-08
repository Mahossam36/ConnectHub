namespace ConnectHub.Models.Entities;

/// <summary>
/// Represents a file uploaded to Connect Hub and optionally associated with a <see cref="Post"/>.
/// The upload workflow is: client uploads the file via <c>POST /api/attachments</c> to receive an ID,
/// then references that ID in <c>CreatePostRequest.attachmentIds</c> when creating a post.
/// The file itself is stored on the server; only the path is persisted in the database.
/// <see cref="PostId"/> is null immediately after upload and is set when the post is created.
/// </summary>
public class Attachment
{

    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid UploadedById { get; set; }
    public Guid? PostId { get; set; }
    public DateTime UploadedAt { get; set; }
    public User UploadedBy { get; set; } = null!;
    public Post? Post { get; set; }
}
