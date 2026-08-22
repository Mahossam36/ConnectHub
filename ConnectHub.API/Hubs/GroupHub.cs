using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.API.Hubs;

[Authorize]
public class GroupHub : Hub
{
    public async Task JoinGroupRoom(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
    }

    public async Task LeaveGroupRoom(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
    }

    public async Task JoinPostRoom(string postId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"post_{postId}");
    }

    public async Task LeavePostRoom(string postId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post_{postId}");
    }
}
