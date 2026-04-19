using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Notifications.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Notifications.Queries.GetNotificationCount;

public class GetNotificationCountQueryHandler : IRequestHandler<GetNotificationCountQuery, NotificationCountDto>
{
    private readonly IListRepository _listRepository;
    private readonly IOptionClaimRepository _claimRepository;
    private readonly IListInviteRepository _inviteRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationCountQueryHandler(
        IListRepository listRepository,
        IOptionClaimRepository claimRepository,
        IListInviteRepository inviteRepository,
        ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _claimRepository = claimRepository;
        _inviteRepository = inviteRepository;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationCountDto> Handle(GetNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        var pendingClaims = await _claimRepository.GetPendingClaimsCountForListAsync(request.ListId, cancellationToken);
        var pendingInvites = await _inviteRepository.GetPendingInvitesCountAsync(request.ListId, cancellationToken);

        return new NotificationCountDto
        {
            PendingClaimsCount = pendingClaims,
            PendingInvitesCount = pendingInvites,
            TotalNew = pendingClaims + pendingInvites
        };
    }
}
