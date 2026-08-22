using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Posts;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[ApiController]
public class PostsController : BaseApiController
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    /// <summary>
    /// Get paginated posts feed for a specific group.
    /// </summary>
    [HttpGet("api/groups/{groupId:guid}/posts")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResultDto<PostResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<PostResponseDto>>> GetGroupFeed(
        Guid groupId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetRequiredUserId();
        var pagination = new PaginationParams { Skip = skip, Take = take };
        var result = await _postService.GetGroupFeedAsync(groupId, currentUserId, pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get a post by its ID.
    /// </summary>
    [HttpGet("api/posts/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostResponseDto>> GetPostById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _postService.GetPostByIdAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Create a new post in a group.
    /// </summary>
    [HttpPost("api/groups/{groupId:guid}/posts")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PostResponseDto>> CreatePost(
        Guid groupId,
        [FromBody] CreatePostRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _postService.CreatePostAsync(groupId, currentUserId, request, cancellationToken);
        return ToCreatedResult(result);
    }

    /// <summary>
    /// Update an authored post.
    /// </summary>
    [HttpPut("api/posts/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostResponseDto>> UpdatePost(
        Guid id,
        [FromBody] UpdatePostRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _postService.UpdatePostAsync(id, currentUserId, request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Delete a post (Author, Group Admin, or Group Owner).
    /// </summary>
    [HttpDelete("api/posts/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletePost(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _postService.DeletePostAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Pin a post to the top of the group feed (Group Admin or Owner).
    /// </summary>
    [HttpPost("api/posts/{id:guid}/pin")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PinPost(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _postService.PinPostAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Unpin a post from the top of the group feed.
    /// </summary>
    [HttpDelete("api/posts/{id:guid}/pin")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnpinPost(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _postService.UnpinPostAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Like a post.
    /// </summary>
    [HttpPost("api/posts/{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> LikePost(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _postService.LikePostAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Remove like from a post.
    /// </summary>
    [HttpDelete("api/posts/{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnlikePost(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _postService.UnlikePostAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }
}
