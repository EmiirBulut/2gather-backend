using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Members.Commands.InviteMember;

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, InviteDto>
{
    private readonly IListRepository _listRepository;
    private readonly IListInviteRepository _inviteRepository;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public InviteMemberCommandHandler(
        IListRepository listRepository,
        IListInviteRepository inviteRepository,
        IEmailService emailService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _listRepository = listRepository;
        _inviteRepository = inviteRepository;
        _emailService = emailService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<InviteDto> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdWithMembersAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var callerMember = list.Members.FirstOrDefault(m => m.UserId == _currentUserService.UserId);
        ListAuthorizationHelper.RequireRole(callerMember, MemberRole.Owner, MemberRole.Editor);

        var token = Guid.NewGuid().ToString("N");
        var invite = new ListInvite
        {
            Id = Guid.NewGuid(),
            ListId = request.ListId,
            InvitedEmail = request.Email,
            Token = token,
            Role = request.Role,
            ExpiresAt = _dateTimeService.UtcNow.AddHours(48),
            CreatedAt = _dateTimeService.UtcNow
        };

        await _inviteRepository.AddAsync(invite, cancellationToken);
        await _inviteRepository.SaveChangesAsync(cancellationToken);

        await _emailService.SendInviteEmailAsync(request.Email, list.Name, token, cancellationToken);

        return new InviteDto(invite.Id, invite.InvitedEmail, invite.Token, invite.ExpiresAt);
    }
}
