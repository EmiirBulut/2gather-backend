using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace TwoGather.Application.Features.Items.Commands.MarkItemPurchased;

public class MarkItemPurchasedCommandHandler : IRequestHandler<MarkItemPurchasedCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<MarkItemPurchasedCommandHandler> _logger;

    public MarkItemPurchasedCommandHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ILogger<MarkItemPurchasedCommandHandler> logger)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task Handle(MarkItemPurchasedCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        if (item.Status == ItemStatus.Purchased)
            throw new DomainException("Item is already marked as purchased.");

        var purchasedAt = _dateTimeService.UtcNow;
        item.Status = ItemStatus.Purchased;
        item.PurchasedAt = purchasedAt;

        await _itemRepository.UpdateAsync(item, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Item {ItemId} marked as purchased by user {UserId}", item.Id, _currentUserService.UserId);

        await _notificationService.ItemPurchasedAsync(item.ListId, item.Id, purchasedAt, cancellationToken);
    }
}
