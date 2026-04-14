using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Options.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Options.Commands.SelectOption;

public class SelectOptionCommandHandler : IRequestHandler<SelectOptionCommand, ItemOptionDto>
{
    private readonly IOptionRepository _optionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public SelectOptionCommandHandler(
        IOptionRepository optionRepository,
        IItemRepository itemRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _optionRepository = optionRepository;
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    public async Task<ItemOptionDto> Handle(SelectOptionCommand request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var item = await _itemRepository.GetByIdAsync(option.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), option.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        var siblings = await _optionRepository.GetByItemIdAsync(option.ItemId, cancellationToken);

        // Deselect all, then select the target
        var updated = siblings.Select(o =>
        {
            o.IsSelected = o.Id == request.OptionId;
            return o;
        }).ToList();

        await _optionRepository.UpdateRangeAsync(updated, cancellationToken);
        await _optionRepository.SaveChangesAsync(cancellationToken);

        var selectedOption = updated.First(o => o.Id == request.OptionId);
        var dto = new ItemOptionDto(selectedOption.Id, selectedOption.ItemId, selectedOption.Title, selectedOption.Price, selectedOption.Currency, selectedOption.Link, selectedOption.Notes, selectedOption.IsSelected, selectedOption.CreatedAt, selectedOption.Brand, selectedOption.Model, selectedOption.Color);

        await _notificationService.OptionUpdatedAsync(item.ListId, item.Id, dto, cancellationToken);

        return dto;
    }
}
