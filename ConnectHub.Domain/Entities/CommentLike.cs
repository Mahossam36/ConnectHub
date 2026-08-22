namespace ConnectHub.Models.Entities;

/// <summary>User like on a comment.</summary>
public class CommentLike
{
    /// <summary>Comment FK.</summary>
    public Guid CommentId { get; set; }

    /// <summary>User FK.</summary>
    public Guid UserId { get; set; }

    public DateTime LikedAt { get; set; }

    /// <summary>The liked comment.</summary>
    public Comment Comment { get; set; } = null!;

    /// <summary>The user who liked.</summary>
    public User User { get; set; } = null!;
}
