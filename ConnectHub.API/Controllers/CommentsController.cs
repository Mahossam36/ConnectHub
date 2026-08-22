using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Comments;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[ApiController]
public class CommentsController : BaseApiController
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    /// <summary>
    /// Get paginated comments and nested replies for a post.
    /// </summary>
    [HttpGet("api/posts/{postId:guid}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResultDto<CommentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResultDto<CommentResponseDto>>> GetPostComments(
        Guid postId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetRequiredUserId();
        var pagination = new PaginationParams { Skip = skip, Take = take };
        var result = await _commentService.GetPostCommentsAsync(postId, currentUserId, pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Add a comment or reply to a post.
    /// </summary>
    [HttpPost("api/posts/{postId:guid}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(CommentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentResponseDto>> AddComment(
        Guid postId,
        [FromBody] CreateCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _commentService.AddCommentAsync(postId, currentUserId, request, cancellationToken);
        return ToCreatedResult(result);
    }

    /// <summary>
    /// Update an authored comment.
    /// </summary>
    [HttpPut("api/comments/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CommentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentResponseDto>> UpdateComment(
        Guid id,
        [FromBody] UpdateCommentRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _commentService.UpdateCommentAsync(id, currentUserId, request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Delete a comment (Author, Group Admin, or Group Owner).
    /// </summary>
    [HttpDelete("api/comments/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteComment(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _commentService.DeleteCommentAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Like a comment.
    /// </summary>
    [HttpPost("api/comments/{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> LikeComment(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _commentService.LikeCommentAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Remove like from a comment.
    /// </summary>
    [HttpDelete("api/comments/{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnlikeComment(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _commentService.UnlikeCommentAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }
}
