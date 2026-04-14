using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Application.Features.Options.DTOs;

namespace TwoGather.Application.Common.Interfaces;

public interface INotificationService
{
    Task ItemAddedAsync(Guid listId, ItemDto item, CancellationToken cancellationToken = default);
    Task ItemUpdatedAsync(Guid listId, ItemDto item, CancellationToken cancellationToken = default);
    Task ItemPurchasedAsync(Guid listId, Guid itemId, DateTime purchasedAt, CancellationToken cancellationToken = default);
    Task ItemDeletedAsync(Guid listId, Guid itemId, CancellationToken cancellationToken = default);
    Task OptionAddedAsync(Guid listId, Guid itemId, ItemOptionDto option, CancellationToken cancellationToken = default);
    Task OptionUpdatedAsync(Guid listId, Guid itemId, ItemOptionDto option, CancellationToken cancellationToken = default);
    Task OptionDeletedAsync(Guid listId, Guid itemId, Guid optionId, CancellationToken cancellationToken = default);
    Task MemberJoinedAsync(Guid listId, MemberDto member, CancellationToken cancellationToken = default);
    Task MemberRemovedAsync(Guid listId, Guid userId, CancellationToken cancellationToken = default);
    Task OptionRatingUpdatedAsync(Guid listId, Guid optionId, CancellationToken cancellationToken = default);
    Task OptionFinalizedAsync(Guid listId, Guid itemId, Guid finalOptionId, CancellationToken cancellationToken = default);
    Task OptionFinalRemovedAsync(Guid listId, Guid itemId, Guid optionId, CancellationToken cancellationToken = default);
}
