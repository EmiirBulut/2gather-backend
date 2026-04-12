using MediatR;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Items.Queries.GetItemsByList;

public record GetItemsByListQuery(Guid ListId, ItemStatus? Status) : IRequest<IReadOnlyList<ItemDto>>;
