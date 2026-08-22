namespace ConnectHub.Models.Entities;

/// <summary>Business profile of a user.</summary>
public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImagePath { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Groups created by this user.</summary>
    public ICollection<Group> CreatedGroups { get; set; } = new List<Group>();

    /// <summary>User's group memberships.</summary>
    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();

    /// <summary>Posts authored by the user.</summary>
    public ICollection<Post> Posts { get; set; } = new List<Post>();

    /// <summary>Comments written by the user.</summary>
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    /// <summary>Post likes placed by the user.</summary>
    public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();

    /// <summary>Comment likes placed by the user.</summary>
    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();

    /// <summary>Attachments uploaded by the user.</summary>
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    /// <summary>Notifications for this user.</summary>
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    /// <summary>Reports filed by this user.</summary>
    public ICollection<Report> Reports { get; set; } = new List<Report>();

    /// <summary>Active and historical refresh tokens belonging to this user.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>Audit log entries attributed to this user.</summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}