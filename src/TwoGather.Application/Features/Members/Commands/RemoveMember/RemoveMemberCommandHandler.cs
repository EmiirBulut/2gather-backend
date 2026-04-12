using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Members.Commands.RemoveMember;

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand>
{
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public RemoveMemberCommandHandler(IListRepository listRepository, ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdWithMembersAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var callerMember = list.Members.FirstOrDefault(m => m.UserId == _currentUserService.UserId);
        ListAuthorizationHelper.RequireRole(callerMember, MemberRole.Owner);

        if (request.UserId == _currentUserService.UserId)
            throw new DomainException("Owner cannot remove themselves from the list.");

        var targetMember = list.Members.FirstOrDefault(m => m.UserId == request.UserId)
            ?? throw new NotFoundException(nameof(Domain.Entities.ListMember), request.UserId);

        await _listRepository.RemoveMemberAsync(targetMember, cancellationToken);
        await _listRepository.SaveChangesAsync(cancellationToken);
    }
}
