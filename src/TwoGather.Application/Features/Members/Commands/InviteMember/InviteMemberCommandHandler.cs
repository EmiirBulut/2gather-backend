using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<InviteMemberCommandHandler> _logger;

    public InviteMemberCommandHandler(
        IListRepository listRepository,
        IListInviteRepository inviteRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ILogger<InviteMemberCommandHandler> logger)
    {
        _listRepository = listRepository;
        _inviteRepository = inviteRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<InviteDto> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdWithMembersAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var callerMember = list.Members.FirstOrDefault(m => m.UserId == _currentUserService.UserId);
        ListAuthorizationHelper.RequireRole(callerMember, MemberRole.Owner, MemberRole.Editor);

        var inviter = await _userRepository.GetByIdAsync(_currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUserService.UserId);

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

        try
        {
            await _emailService.SendInviteAsync(request.Email, list.Name, inviter.DisplayName, token, request.Role, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invite created but email delivery failed for {Email} on list {ListId}", request.Email, request.ListId);
        }

        return new InviteDto(invite.Id, invite.InvitedEmail, invite.Token, invite.ExpiresAt);
    }
}
