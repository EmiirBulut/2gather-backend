using MediatR;
using TwoGather.Application.Features.Options.DTOs;

namespace TwoGather.Application.Features.Options.Queries.GetOptionsByItem;

public record GetOptionsByItemQuery(Guid ItemId) : IRequest<IReadOnlyList<ItemOptionDto>>;
