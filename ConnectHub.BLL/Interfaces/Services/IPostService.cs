using Ardalis.Result;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Posts;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IPostService
{
    Task<Result<PagedResultDto<PostResponseDto>>> GetGroupFeedAsync(Guid groupId, Guid currentUserId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Result<PostResponseDto>> GetPostByIdAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<PostResponseDto>> CreatePostAsync(Guid groupId, Guid currentUserId, CreatePostRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<PostResponseDto>> UpdatePostAsync(Guid postId, Guid currentUserId, UpdatePostRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> DeletePostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> PinPostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> UnpinPostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> LikePostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> UnlikePostAsync(Guid postId, Guid currentUserId, CancellationToken cancellationToken = default);
}
