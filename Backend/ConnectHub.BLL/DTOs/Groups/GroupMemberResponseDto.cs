using ConnectHub.BLL.DTOs.Users;
using ConnectHub.Models.Enums;

namespace ConnectHub.BLL.DTOs.Groups;

public class GroupMemberResponseDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public UserSummaryDto User { get; set; } = null!;
    public GroupRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
