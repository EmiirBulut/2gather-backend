using Microsoft.AspNetCore.SignalR;

namespace TwoGather.Api.Hubs;

public class ListHub : Hub
{
    public async Task JoinList(string listId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"list-{listId}");

    public async Task LeaveList(string listId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"list-{listId}");
}
