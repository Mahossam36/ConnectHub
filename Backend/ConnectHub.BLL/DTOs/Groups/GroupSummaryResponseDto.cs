using ConnectHub.Models.Enums;

namespace ConnectHub.BLL.DTOs.Groups;

public class GroupSummaryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public CategoryDto Category { get; set; } = null!;
    public List<TagDto> Tags { get; set; } = new();
    public int MemberCount { get; set; }
    public GroupRole? CurrentUserRole { get; set; }
    public DateTime CreatedAt { get; set; }
}
