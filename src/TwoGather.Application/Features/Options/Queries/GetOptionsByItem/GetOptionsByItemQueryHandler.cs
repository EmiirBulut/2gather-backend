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

        var options = await _optionRepository.GetByItemIdWithRatingsAsync(request.ItemId, _currentUserService.UserId, cancellationToken);

        return options.Select(x => new ItemOptionDto(
            x.option.Id,
            x.option.ItemId,
            x.option.Title,
            x.option.Price,
            x.option.Currency,
            x.option.Link,
            x.option.Notes,
            x.option.IsSelected,
            x.option.CreatedAt,
            x.option.Brand,
            x.option.Model,
            x.option.Color,
            x.averageRating,
            x.totalRatings,
            x.currentUserScore,
            x.option.IsFinal,
            x.option.FinalizedAt
        )).ToList();
    }
}
