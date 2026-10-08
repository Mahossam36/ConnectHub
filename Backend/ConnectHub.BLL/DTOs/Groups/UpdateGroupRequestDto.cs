namespace ConnectHub.BLL.DTOs.Groups;

public class UpdateGroupRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = new();
    public string? CoverImageUrl { get; set; }
}
