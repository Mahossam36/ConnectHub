using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IGroupMemberService
{
    Task<PagedResultDto<GroupMemberResponseDto>> GetMembersAsync(Guid groupId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task JoinGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task LeaveGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task ChangeMemberRoleAsync(Guid groupId, Guid targetUserId, Guid currentUserId, ChangeMemberRoleRequestDto request, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid groupId, Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken = default);
}
