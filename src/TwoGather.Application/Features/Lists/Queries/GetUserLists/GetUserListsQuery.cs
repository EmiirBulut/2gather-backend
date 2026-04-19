using MediatR;
using TwoGather.Application.Features.Lists.DTOs;

namespace TwoGather.Application.Features.Lists.Queries.GetUserLists;

public record GetUserListsQuery : IRequest<IReadOnlyList<ListSummaryDto>>;
