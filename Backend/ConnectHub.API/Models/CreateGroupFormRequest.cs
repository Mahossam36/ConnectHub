using ConnectHub.BLL.DTOs.Groups;

namespace ConnectHub.API.Models;

public class CreateGroupFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = new();
    public IFormFile? CoverImage { get; set; }

    public CreateGroupRequestDto ToDto() => new()
    {
        Name = Name,
        Description = Description,
        CategoryId = CategoryId,
        TagIds = TagIds
    };
}
