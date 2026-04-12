using MediatR;

namespace TwoGather.Application.Features.Items.Commands.MarkItemPurchased;

public record MarkItemPurchasedCommand(Guid ItemId) : IRequest;
