using Ardalis.Result;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Comments;

namespace ConnectHub.BLL.Interfaces.Services;

public interface ICommentService
{
    Task<Result<PagedResultDto<CommentResponseDto>>> GetPostCommentsAsync(Guid postId, Guid currentUserId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Result<CommentResponseDto>> AddCommentAsync(Guid postId, Guid currentUserId, CreateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<CommentResponseDto>> UpdateCommentAsync(Guid commentId, Guid currentUserId, UpdateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> DeleteCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> LikeCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result> UnlikeCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
}
