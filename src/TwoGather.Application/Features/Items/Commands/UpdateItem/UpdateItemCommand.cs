using MediatR;
using TwoGather.Application.Features.Items.DTOs;

namespace TwoGather.Application.Features.Items.Commands.UpdateItem;

public record UpdateItemCommand(Guid ItemId, string Name, Guid CategoryId) : IRequest<ItemDto>;
