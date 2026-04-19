using MediatR;
using TwoGather.Application.Features.Items.DTOs;

namespace TwoGather.Application.Features.Items.Queries.GetItemDetail;

public record GetItemDetailQuery(Guid ItemId) : IRequest<ItemDetailDto>;
