using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Members.Queries.GetPendingInvites;

public class GetPendingInvitesQueryHandler : IRequestHandler<GetPendingInvitesQuery, IReadOnlyList<PendingInviteDto>>
{
    private readonly IListRepository _listRepository;
    private readonly IListInviteRepository _inviteRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public GetPendingInvitesQueryHandler(
        IListRepository listRepository,
        IListInviteRepository inviteRepository,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _listRepository = listRepository;
        _inviteRepository = inviteRepository;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<IReadOnlyList<PendingInviteDto>> Handle(GetPendingInvitesQuery request, CancellationToken cancellationToken)
    {
        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        var invites = await _inviteRepository.GetByListIdAsync(request.ListId, cancellationToken);
        var now = _dateTimeService.UtcNow;

        return invites.Select(i => new PendingInviteDto
        {
            InviteId = i.Id,
            InvitedEmail = i.InvitedEmail,
            Role = i.Role,
            ExpiresAt = i.ExpiresAt,
            CreatedAt = i.CreatedAt,
            IsExpired = i.ExpiresAt < now
        }).ToList();
    }
}
