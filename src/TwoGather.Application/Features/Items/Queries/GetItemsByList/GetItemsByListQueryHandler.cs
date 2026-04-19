using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Items.Queries.GetItemsByList;

public class GetItemsByListQueryHandler : IRequestHandler<GetItemsByListQuery, IReadOnlyList<ItemDto>>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetItemsByListQueryHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        ICurrentUserService currentUserService)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ItemDto>> Handle(GetItemsByListQuery request, CancellationToken cancellationToken)
    {
        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        var items = await _itemRepository.GetByListIdAsync(request.ListId, request.Status, cancellationToken);

        return items.Select(x => new ItemDto(
            x.item.Id,
            x.item.ListId,
            x.item.CategoryId,
            x.item.Name,
            x.item.Status,
            x.item.PurchasedAt,
            x.item.ImageUrl,
            x.item.PlanningNote,
            x.item.CreatedAt,
            x.item.UpdatedAt,
            x.optionsCount
        )).ToList();
    }
}
