using MediatR;
using TwoGather.Application.Features.Lists.DTOs;

namespace TwoGather.Application.Features.Lists.Queries.GetListById;

public record GetListByIdQuery(Guid ListId) : IRequest<ListDetailDto>;
