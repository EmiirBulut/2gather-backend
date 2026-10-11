using MediatR;

namespace TwoGather.Application.Features.Items.Commands.UndoPurchase;

public record UndoPurchaseCommand(Guid ItemId) : IRequest;
