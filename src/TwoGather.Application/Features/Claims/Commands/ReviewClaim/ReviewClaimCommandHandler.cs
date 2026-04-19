using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Claims.DTOs;
using TwoGather.Application.Features.Notifications.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Claims.Commands.ReviewClaim;

public class ReviewClaimCommandHandler : IRequestHandler<ReviewClaimCommand, ClaimDto>
{
    private readonly IOptionClaimRepository _claimRepository;
    private readonly IOptionRepository _optionRepository;
    private readonly IListRepository _listRepository;
    private readonly IListInviteRepository _inviteRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public ReviewClaimCommandHandler(
        IOptionClaimRepository claimRepository,
        IOptionRepository optionRepository,
        IListRepository listRepository,
        IListInviteRepository inviteRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _claimRepository = claimRepository;
        _optionRepository = optionRepository;
        _listRepository = listRepository;
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ClaimDto> Handle(ReviewClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.OptionClaim), request.ClaimId);

        var option = await _optionRepository.GetByIdWithItemAsync(claim.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), claim.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        if (claim.Status != ClaimStatus.Pending)
            throw new DomainException("Yalnızca beklemedeki talepler incelenebilir.");

        if (request.Decision == ClaimStatus.Approved)
        {
            var approvedTotal = await _claimRepository.GetApprovedPercentageTotalAsync(claim.OptionId, cancellationToken);
            if (approvedTotal + claim.Percentage > 100)
                throw new DomainException($"Talep onaylanamaz. Kalan kapasite: {100 - approvedTotal}%");
        }

        claim.Status = request.Decision;
        claim.ReviewedAt = _dateTimeService.UtcNow;
        claim.ReviewedBy = _currentUserService.UserId;

        await _claimRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(claim.UserId, cancellationToken);
        var dto = new ClaimDto(claim.Id, claim.UserId, user?.DisplayName ?? string.Empty, claim.Percentage, claim.Status, claim.CreatedAt);

        await _notificationService.ClaimReviewedAsync(option.Item.ListId, claim.OptionId, dto, cancellationToken);

        var pendingClaims = await _claimRepository.GetPendingClaimsCountForListAsync(option.Item.ListId, cancellationToken);
        var pendingInvites = await _inviteRepository.GetPendingInvitesCountAsync(option.Item.ListId, cancellationToken);
        var countDto = new NotificationCountDto
        {
            PendingClaimsCount = pendingClaims,
            PendingInvitesCount = pendingInvites,
            TotalNew = pendingClaims + pendingInvites
        };
        await _notificationService.NotificationCountChangedAsync(option.Item.ListId, _currentUserService.UserId, countDto, cancellationToken);

        return dto;
    }
}
