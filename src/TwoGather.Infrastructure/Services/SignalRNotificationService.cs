using Microsoft.AspNetCore.SignalR;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Application.Features.Options.DTOs;

namespace TwoGather.Infrastructure.Services;

public class SignalRNotificationService<THub> : INotificationService where THub : Hub
{
    private readonly IHubContext<THub> _hubContext;

    public SignalRNotificationService(IHubContext<THub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task ItemAddedAsync(Guid listId, ItemDto item, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("ItemAdded", new { listId, item }, cancellationToken);

    public Task ItemUpdatedAsync(Guid listId, ItemDto item, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("ItemUpdated", new { listId, item }, cancellationToken);

    public Task ItemPurchasedAsync(Guid listId, Guid itemId, DateTime purchasedAt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("ItemPurchased", new { listId, itemId, purchasedAt }, cancellationToken);

    public Task ItemDeletedAsync(Guid listId, Guid itemId, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("ItemDeleted", new { listId, itemId }, cancellationToken);

    public Task OptionAddedAsync(Guid listId, Guid itemId, ItemOptionDto option, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("OptionAdded", new { listId, itemId, option }, cancellationToken);

    public Task OptionUpdatedAsync(Guid listId, Guid itemId, ItemOptionDto option, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("OptionUpdated", new { listId, itemId, option }, cancellationToken);

    public Task OptionDeletedAsync(Guid listId, Guid itemId, Guid optionId, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("OptionDeleted", new { listId, itemId, optionId }, cancellationToken);

    public Task MemberJoinedAsync(Guid listId, MemberDto member, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("MemberJoined", new { listId, member }, cancellationToken);

    public Task MemberRemovedAsync(Guid listId, Guid userId, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("MemberRemoved", new { listId, userId }, cancellationToken);

    public Task OptionRatingUpdatedAsync(Guid listId, Guid optionId, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("OptionRatingUpdated", new { listId, optionId }, cancellationToken);
}
