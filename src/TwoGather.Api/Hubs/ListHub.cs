using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TwoGather.Application.Common.Interfaces;

namespace TwoGather.Api.Hubs;

[Authorize]
public class ListHub : Hub
{
    private readonly IListRepository _listRepository;

    public ListHub(IListRepository listRepository)
    {
        _listRepository = listRepository;
    }

    public async Task JoinList(string listId)
    {
        if (!Guid.TryParse(listId, out var listGuid))
        {
            throw new HubException("Invalid list ID.");
        }

        var userId = GetUserId();
        var member = await _listRepository.GetMemberAsync(listGuid, userId);

        if (member is null)
            throw new HubException("You are not a member of this list.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"list-{listId}");
    }

    public async Task LeaveList(string listId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"list-{listId}");
    }

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value is null || !Guid.TryParse(value, out var id))
            throw new HubException("Unauthenticated.");
        return id;
    }
}
