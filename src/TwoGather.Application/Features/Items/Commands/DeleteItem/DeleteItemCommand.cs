using MediatR;

namespace TwoGather.Application.Features.Items.Commands.DeleteItem;

public record DeleteItemCommand(Guid ItemId) : IRequest;
