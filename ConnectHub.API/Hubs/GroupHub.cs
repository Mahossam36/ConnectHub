using System.Security.Claims;
using ConnectHub.DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.API.Hubs;

[Authorize]
public class GroupHub : Hub
{
    private readonly IGroupRepository _groupRepository;
    private readonly IPostRepository _postRepository;

    public GroupHub(IGroupRepository groupRepository, IPostRepository postRepository)
    {
        _groupRepository = groupRepository;
        _postRepository = postRepository;
    }

    public async Task JoinGroupRoom(string groupId)
    {
        var (parsedGroupId, userId) = GetRequiredIds(groupId);
        if (!await _groupRepository.IsUserMemberAsync(parsedGroupId, userId))
            throw new HubException("You must be an active group member to join this room.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
    }

    public async Task LeaveGroupRoom(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
    }

    public async Task JoinPostRoom(string postId)
    {
        var (parsedPostId, userId) = GetRequiredIds(postId);
        var post = await _postRepository.GetByIdAsync(parsedPostId);
        if (post is null)
            throw new HubException("Post not found.");
        if (!await _groupRepository.IsUserMemberAsync(post.GroupId, userId))
            throw new HubException("You must be an active group member to join this room.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"post_{postId}");
    }

    public async Task LeavePostRoom(string postId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post_{postId}");
    }

    private (Guid ResourceId, Guid UserId) GetRequiredIds(string resourceId)
    {
        if (!Guid.TryParse(resourceId, out var parsedResourceId))
            throw new HubException("The room ID must be a GUID.");

        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new HubException("User is not authenticated.");

        return (parsedResourceId, userId);
    }
}
