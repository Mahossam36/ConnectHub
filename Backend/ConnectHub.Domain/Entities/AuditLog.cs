namespace ConnectHub.Models.Entities;

/// <summary>
/// Records important security and business actions for audit trail purposes.
/// Never contains passwords, tokens, or secrets.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>FK to the User who performed the action. Null for system-initiated actions.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Action name, e.g. "Login", "Register", "CreateGroup", "DeletePost".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>The entity type affected, e.g. "User", "Group", "Post".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>The ID of the entity affected. Null for actions not tied to a specific record.</summary>
    public Guid? EntityId { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>Optional JSON metadata describing the action context.</summary>
    public string? Metadata { get; set; }

    /// <summary>Navigation to the acting user, if applicable.</summary>
    public User? User { get; set; }
}
