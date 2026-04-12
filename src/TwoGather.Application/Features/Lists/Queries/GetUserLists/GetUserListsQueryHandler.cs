using MediatR;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Lists.DTOs;

namespace TwoGather.Application.Features.Lists.Queries.GetUserLists;

public class GetUserListsQueryHandler : IRequestHandler<GetUserListsQuery, IReadOnlyList<ListDto>>
{
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUserListsQueryHandler(IListRepository listRepository, ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ListDto>> Handle(GetUserListsQuery request, CancellationToken cancellationToken)
    {
        var lists = await _listRepository.GetByUserIdAsync(_currentUserService.UserId, cancellationToken);

        return lists.Select(l => new ListDto(
            l.Id,
            l.Name,
            l.OwnerId,
            l.CreatedAt,
            l.Members.Count
        )).ToList();
    }
}
