namespace ConnectHub.Models.Entities;

/// <summary>Comment or reply on a post.</summary>
public class Comment
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }


    /// <summary>Author User FK.</summary>
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;


    /// <summary>Post FK.</summary>
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;


    /// <summary>Parent Comment FK (for replies).</summary>
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }


    /// <summary>Replies to this comment.</summary>
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public int RepliesCount { get; set; }


    /// <summary>Likes on this comment.</summary>
    public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
    public int LikesCount { get; set; }

}
