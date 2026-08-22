using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Posts;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IPostService
{
    Task<PagedResultDto<PostResponseDto>> GetGroupFeedAsync(Guid groupId, Guid currentUserId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<PostResponseDto> GetPostByIdAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<PostResponseDto> CreatePostAsync(Guid groupId, Guid currentUserId, CreatePostRequestDto request, CancellationToken cancellationToken = default);
    Task<PostResponseDto> UpdatePostAsync(Guid postId, Guid currentUserId, UpdatePostRequestDto request, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task PinPostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task UnpinPostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task LikePostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task UnlikePostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
}
