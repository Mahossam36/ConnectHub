using Ardalis.Result;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IGroupMemberService
{
    Task<Result<PagedResultDto<GroupMemberResponseDto>>> GetMembersAsync(Guid groupId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Result> JoinGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> LeaveGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> ChangeMemberRoleAsync(Guid groupId, Guid targetUserId, Guid currentUserId, ChangeMemberRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> RemoveMemberAsync(Guid groupId, Guid targetUserId, Guid currentUserId, CancellationToken cancellationToken = default);
}
