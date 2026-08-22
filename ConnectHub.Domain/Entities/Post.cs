namespace ConnectHub.Models.Entities;

/// <summary>Post in a group feed.</summary>
public class Post
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; }

    /// <summary>Author User FK.</summary>
    public Guid AuthorId { get; set; }

    /// <summary>Group FK.</summary>
    public Guid GroupId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Author { get; set; } = null!;
    public Group Group { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
