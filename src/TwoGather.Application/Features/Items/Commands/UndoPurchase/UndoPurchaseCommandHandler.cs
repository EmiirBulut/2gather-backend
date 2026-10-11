using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Items.Commands.UndoPurchase;

/// <summary>Owner-only correction: puts a purchased item back to pending and unlocks it.</summary>
public class UndoPurchaseCommandHandler : IRequestHandler<UndoPurchaseCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<UndoPurchaseCommandHandler> _logger;

    public UndoPurchaseCommandHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ILogger<UndoPurchaseCommandHandler> logger)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task Handle(UndoPurchaseCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        if (item.Status != ItemStatus.Purchased)
            throw new DomainException("Bu ürün satın alınmış olarak işaretli değil.");

        item.Status = ItemStatus.Pending;
        item.PurchasedAt = null;
        item.UpdatedAt = _dateTimeService.UtcNow;

        await _itemRepository.UpdateAsync(item, cancellationToken);
        await _itemRepository.RemovePurchasersAsync(item.Id, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Purchase of item {ItemId} undone by user {UserId}", item.Id, _currentUserService.UserId);
        await _notificationService.ItemPurchaseUndoneAsync(item.ListId, item.Id, cancellationToken);
    }
}
