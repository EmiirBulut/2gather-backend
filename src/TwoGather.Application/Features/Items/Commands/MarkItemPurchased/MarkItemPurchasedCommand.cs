using MediatR;

namespace TwoGather.Application.Features.Items.Commands.MarkItemPurchased;

/// <summary>PurchasedOptionId is required when the item has options and must belong to the item.</summary>
public record MarkItemPurchasedCommand(Guid ItemId, IReadOnlyList<Guid> PurchaserUserIds, Guid? PurchasedOptionId) : IRequest;
