namespace ConnectHub.Models.Entities;

/// <summary>User like on a post.</summary>
public class PostLike
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }

    public DateTime LikedAt { get; set; }
    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
