using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Items.Commands.MarkItemPurchased;

public class MarkItemPurchasedCommandHandler : IRequestHandler<MarkItemPurchasedCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly IOptionRepository _optionRepository;
    private readonly IOptionClaimRepository _claimRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<MarkItemPurchasedCommandHandler> _logger;

    public MarkItemPurchasedCommandHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        IOptionRepository optionRepository,
        IOptionClaimRepository claimRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ILogger<MarkItemPurchasedCommandHandler> logger)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _optionRepository = optionRepository;
        _claimRepository = claimRepository;
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

        var finalOption = await _optionRepository.GetCurrentFinalOptionForItemAsync(item.Id, cancellationToken);
        if (finalOption is not null)
        {
            var approvedClaims = await _claimRepository.GetByOptionIdAsync(finalOption.Id, cancellationToken);
            var approvedClaimsList = approvedClaims.Where(c => c.Status == ClaimStatus.Approved).ToList();

            if (approvedClaimsList.Count > 0)
            {
                var isClaimant = approvedClaimsList.Any(c => c.UserId == _currentUserService.UserId);
                if (!isClaimant)
                    throw new ForbiddenException();
            }
            else if (member!.Role != MemberRole.Owner)
            {
                throw new ForbiddenException();
            }
        }
        else if (member!.Role != MemberRole.Owner)
        {
            throw new ForbiddenException();
        }

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
