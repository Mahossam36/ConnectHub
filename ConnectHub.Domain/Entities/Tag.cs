namespace ConnectHub.Models.Entities;

/// <summary>Community group tag.</summary>
public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Groups with this tag.</summary>
    public ICollection<Group> Groups { get; set; } = new List<Group>();
}
