using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[Route("api/[controller]")]
public class GroupsController : BaseApiController
{
    private readonly IGroupService _groupService;
    private readonly IGroupMemberService _groupMemberService;

    public GroupsController(IGroupService groupService, IGroupMemberService groupMemberService)
    {
        _groupService = groupService;
        _groupMemberService = groupMemberService;
    }

    /// <summary>
    /// Browse and search active community groups.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<GroupSummaryResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<GroupSummaryResponseDto>>> BrowseGroups(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? tagId,
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var pagination = new PaginationParams { Skip = skip, Take = take };
        var result = await _groupService.BrowseGroupsAsync(currentUserId, categoryId, tagId, search, pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get details of a specific community group.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GroupDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDetailResponseDto>> GetGroupById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _groupService.GetGroupByIdAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Create a new community group.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Authorize]
    [ProducesResponseType(typeof(GroupDetailResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GroupDetailResponseDto>> CreateGroup(
        [FromForm] CreateGroupFormRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        await using var coverImageStream = request.CoverImage?.OpenReadStream();
        var result = await _groupService.CreateGroupAsync(
            currentUserId,
            request.ToDto(),
            coverImageStream,
            request.CoverImage?.FileName,
            cancellationToken);
        return ToCreatedResult(result);
    }

    /// <summary>
    /// Update settings of a community group (Owner or Admin only).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(GroupDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDetailResponseDto>> UpdateGroup(Guid id, [FromBody] UpdateGroupRequestDto request, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _groupService.UpdateGroupAsync(id, currentUserId, request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Soft delete a community group (Owner only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _groupService.DeleteGroupAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get paginated members list of a group.
    /// </summary>
    [HttpGet("{id:guid}/members")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResultDto<GroupMemberResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResultDto<GroupMemberResponseDto>>> GetMembers(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetRequiredUserId();
        var pagination = new PaginationParams { Skip = skip, Take = take };
        var result = await _groupMemberService.GetMembersAsync(id, currentUserId, pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Join a community group.
    /// </summary>
    [HttpPost("{id:guid}/join")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> JoinGroup(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _groupMemberService.JoinGroupAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Leave a community group.
    /// </summary>
    [HttpPost("{id:guid}/leave")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> LeaveGroup(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _groupMemberService.LeaveGroupAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Change the role of a group member (Owner or Admin).
    /// </summary>
    [HttpPut("{id:guid}/members/{targetUserId:guid}/role")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangeMemberRole(
        Guid id,
        Guid targetUserId,
        [FromBody] ChangeMemberRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _groupMemberService.ChangeMemberRoleAsync(id, targetUserId, currentUserId, request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Remove a member from a group (Owner or Admin).
    /// </summary>
    [HttpDelete("{id:guid}/members/{targetUserId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveMember(Guid id, Guid targetUserId, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _groupMemberService.RemoveMemberAsync(id, targetUserId, currentUserId, cancellationToken);
        return ToActionResult(result);
    }
}
