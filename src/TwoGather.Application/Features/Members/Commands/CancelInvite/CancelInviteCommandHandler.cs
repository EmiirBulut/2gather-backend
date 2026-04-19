using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Members.Commands.CancelInvite;

public class CancelInviteCommandHandler : IRequestHandler<CancelInviteCommand>
{
    private readonly IListRepository _listRepository;
    private readonly IListInviteRepository _inviteRepository;
    private readonly ICurrentUserService _currentUserService;

    public CancelInviteCommandHandler(
        IListRepository listRepository,
        IListInviteRepository inviteRepository,
        ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _inviteRepository = inviteRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(CancelInviteCommand request, CancellationToken cancellationToken)
    {
        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        var invite = await _inviteRepository.GetByIdAsync(request.InviteId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ListInvite), request.InviteId);

        if (invite.ListId != request.ListId)
            throw new NotFoundException(nameof(Domain.Entities.ListInvite), request.InviteId);

        if (invite.AcceptedAt.HasValue)
            throw new DomainException("Kabul edilmiş davet iptal edilemez.");

        await _inviteRepository.DeleteAsync(invite, cancellationToken);
        await _inviteRepository.SaveChangesAsync(cancellationToken);
    }
}
