using Microsoft.AspNetCore.SignalR;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Claims.DTOs;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Application.Features.Notifications.DTOs;
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

    public Task ItemImageUpdatedAsync(Guid listId, Guid itemId, string imageUrl, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("ItemImageUpdated", new { listId, itemId, imageUrl }, cancellationToken);

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

    public Task OptionFinalizedAsync(Guid listId, Guid itemId, Guid finalOptionId, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("OptionFinalized", new { listId, itemId, finalOptionId }, cancellationToken);

    public Task OptionFinalRemovedAsync(Guid listId, Guid itemId, Guid optionId, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("OptionFinalRemoved", new { listId, itemId, optionId }, cancellationToken);

    public Task ClaimCreatedAsync(Guid listId, Guid optionId, ClaimDto claim, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("ClaimCreated", new { listId, optionId, claim }, cancellationToken);

    public Task ClaimReviewedAsync(Guid listId, Guid optionId, ClaimDto claim, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"list-{listId}").SendAsync("ClaimReviewed", new { listId, optionId, claim }, cancellationToken);

    public Task ClaimPendingNotificationAsync(Guid listId, Guid ownerUserId, ClaimDto claim, CancellationToken cancellationToken = default)
        => _hubContext.Clients.User(ownerUserId.ToString()).SendAsync("ClaimPending", new { listId, claim }, cancellationToken);

    public Task NotificationCountChangedAsync(Guid listId, Guid ownerUserId, NotificationCountDto count, CancellationToken cancellationToken = default)
        => _hubContext.Clients.User(ownerUserId.ToString()).SendAsync("NotificationCountChanged", new { listId, count }, cancellationToken);
}
