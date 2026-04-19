using MediatR;
using TwoGather.Application.Features.Items.DTOs;

namespace TwoGather.Application.Features.Items.Commands.CreateItem;

public record CreateItemCommand(Guid ListId, Guid CategoryId, string Name, string? ImageUrl, string? PlanningNote) : IRequest<ItemDto>;
