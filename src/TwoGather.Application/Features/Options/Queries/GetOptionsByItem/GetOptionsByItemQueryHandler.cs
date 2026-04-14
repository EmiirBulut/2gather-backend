using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Options.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Options.Queries.GetOptionsByItem;

public class GetOptionsByItemQueryHandler : IRequestHandler<GetOptionsByItemQuery, IReadOnlyList<ItemOptionDto>>
{
    private readonly IOptionRepository _optionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOptionsByItemQueryHandler(
        IOptionRepository optionRepository,
        IItemRepository itemRepository,
        IListRepository listRepository,
        ICurrentUserService currentUserService)
    {
        _optionRepository = optionRepository;
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ItemOptionDto>> Handle(GetOptionsByItemQuery request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        var options = await _optionRepository.GetByItemIdAsync(request.ItemId, cancellationToken);

        return options.Select(o => new ItemOptionDto(o.Id, o.ItemId, o.Title, o.Price, o.Currency, o.Link, o.Notes, o.IsSelected, o.CreatedAt, o.Brand, o.Model, o.Color)).ToList();
    }
}
