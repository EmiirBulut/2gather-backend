using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Members.Commands.ResendInvite;

public class ResendInviteCommandHandler : IRequestHandler<ResendInviteCommand>
{
    private readonly IListRepository _listRepository;
    private readonly IListInviteRepository _inviteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public ResendInviteCommandHandler(
        IListRepository listRepository,
        IListInviteRepository inviteRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _listRepository = listRepository;
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(ResendInviteCommand request, CancellationToken cancellationToken)
    {
        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        var invite = await _inviteRepository.GetByIdAsync(request.InviteId, cancellationToken)
            ?? throw new NotFoundException(nameof(ListInvite), request.InviteId);

        if (invite.ListId != request.ListId)
            throw new NotFoundException(nameof(ListInvite), request.InviteId);

        if (invite.AcceptedAt.HasValue)
            throw new DomainException("Kabul edilmiş davet yeniden gönderilemez.");

        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var inviter = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUserService.UserId);

        invite.ExpiresAt = _dateTimeService.UtcNow.AddDays(7);

        await _inviteRepository.UpdateAsync(invite, cancellationToken);
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        await _emailService.SendInviteAsync(invite.InvitedEmail, list.Name, inviter.DisplayName, invite.Token, invite.Role, cancellationToken);
    }
}
