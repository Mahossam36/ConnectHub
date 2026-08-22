using Ardalis.Result;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IGroupService
{
    Task<Result<PagedResultDto<GroupSummaryResponseDto>>> BrowseGroupsAsync(Guid? currentUserId, Guid? categoryId, Guid? tagId, string? search, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Result<GroupDetailResponseDto>> GetGroupByIdAsync(Guid groupId, Guid? currentUserId, CancellationToken cancellationToken = default);
    Task<Result<GroupDetailResponseDto>> CreateGroupAsync(Guid currentUserId, CreateGroupRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<GroupDetailResponseDto>> UpdateGroupAsync(Guid groupId, Guid currentUserId, UpdateGroupRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> DeleteGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default);
}
