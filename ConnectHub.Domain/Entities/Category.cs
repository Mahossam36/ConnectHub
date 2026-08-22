namespace ConnectHub.Models.Entities;

/// <summary>
/// Each <see cref="Group"/> belongs to exactly one category (e.g. Sports, Technology, Hobbies).
/// </summary>
public class Category
{

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation
    /// <summary>All groups that belong to this category.</summary>
    public ICollection<Group> Groups { get; set; } = new List<Group>();
}
