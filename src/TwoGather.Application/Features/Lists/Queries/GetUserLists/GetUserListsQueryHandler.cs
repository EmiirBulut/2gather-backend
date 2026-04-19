using MediatR;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Lists.DTOs;

namespace TwoGather.Application.Features.Lists.Queries.GetUserLists;

public class GetUserListsQueryHandler : IRequestHandler<GetUserListsQuery, IReadOnlyList<ListSummaryDto>>
{
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUserListsQueryHandler(IListRepository listRepository, ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ListSummaryDto>> Handle(GetUserListsQuery request, CancellationToken cancellationToken)
    {
        return await _listRepository.GetUserListsSummaryAsync(_currentUserService.UserId, cancellationToken);
    }
}
