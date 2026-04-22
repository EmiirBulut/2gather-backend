using MediatR;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Members.Commands.AcceptInvite;

public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, MemberDto>
{
    private readonly IListInviteRepository _inviteRepository;
    private readonly IListRepository _listRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AcceptInviteCommandHandler(
        IListInviteRepository inviteRepository,
        IListRepository listRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _inviteRepository = inviteRepository;
        _listRepository = listRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<MemberDto> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _inviteRepository.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new NotFoundException(nameof(ListInvite), request.Token);

        if (invite.AcceptedAt is not null)
            throw new DomainException("Invite has already been accepted.");

        if (invite.ExpiresAt < _dateTimeService.UtcNow)
            throw new DomainException("Invite token has expired.");

        var currentUserId = _currentUserService.UserId;

        var existingMember = await _listRepository.GetMemberAsync(invite.ListId, currentUserId, cancellationToken);
        if (existingMember is not null)
            throw new DomainException("You are already a member of this list.");

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), currentUserId);

        if (!string.Equals(user.Email, invite.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Bu davet size ait değil.");

        var now = _dateTimeService.UtcNow;

        var member = new ListMember
        {
            Id = Guid.NewGuid(),
            ListId = invite.ListId,
            UserId = currentUserId,
            Role = invite.Role,
            JoinedAt = now
        };

        await _listRepository.AddMemberAsync(member, cancellationToken);

        var acceptedInvite = new ListInvite
        {
            Id = invite.Id,
            ListId = invite.ListId,
            InvitedEmail = invite.InvitedEmail,
            Token = invite.Token,
            Role = invite.Role,
            ExpiresAt = invite.ExpiresAt,
            AcceptedAt = now,
            CreatedAt = invite.CreatedAt
        };
        await _inviteRepository.UpdateAcceptedAtAsync(acceptedInvite, cancellationToken);

        // Both repos share the same DbContext — one SaveChanges commits all tracked changes
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        return new MemberDto(currentUserId, user.DisplayName, user.Email, member.Role, member.JoinedAt);
    }
}
