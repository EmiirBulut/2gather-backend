using MediatR;
using TwoGather.Application.Common.Pricing;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Items.Commands.UpdateItem;

public class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand, ItemDto>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public UpdateItemCommandHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ItemDto> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdWithOptionsAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        item.Name = request.Name;
        item.CategoryId = request.CategoryId;
        item.ImageUrl = request.ImageUrl;
        item.PlanningNote = request.PlanningNote;
        item.UpdatedAt = _dateTimeService.UtcNow;

        await _itemRepository.UpdateAsync(item, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        var dto = new ItemDto(item.Id, item.ListId, item.CategoryId, item.Name, item.Status, item.PurchasedAt, item.ImageUrl, item.PlanningNote, item.CreatedAt, item.UpdatedAt, item.Options.Count,
            EffectiveOption.Pick(item.Options.Select(o => new OptionPriceInfo(o.Title, o.Price, o.Currency, o.IsFinal, o.IsSelected)))?.Price);

        await _notificationService.ItemUpdatedAsync(item.ListId, dto, cancellationToken);

        return dto;
    }
}
