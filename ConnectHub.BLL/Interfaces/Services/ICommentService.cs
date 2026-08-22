using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Comments;

namespace ConnectHub.BLL.Interfaces.Services;

public interface ICommentService
{
    Task<PagedResultDto<CommentResponseDto>> GetPostCommentsAsync(Guid postId, Guid currentUserId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<CommentResponseDto> AddCommentAsync(Guid postId, Guid currentUserId, CreateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task<CommentResponseDto> UpdateCommentAsync(Guid commentId, Guid currentUserId, UpdateCommentRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task LikeCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task UnlikeCommentAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
}
