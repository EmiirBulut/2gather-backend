using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Members.Commands.UpdateMemberRole;

public class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, MemberDto>
{
    private readonly IListRepository _listRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMemberRoleCommandHandler(
        IListRepository listRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MemberDto> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdWithMembersAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var callerMember = list.Members.FirstOrDefault(m => m.UserId == _currentUserService.UserId);
        ListAuthorizationHelper.RequireRole(callerMember, MemberRole.Owner);

        if (request.UserId == _currentUserService.UserId)
            throw new DomainException("Owner cannot change their own role.");

        if (request.Role == MemberRole.Owner)
            throw new DomainException("Cannot assign Owner role via this endpoint.");

        var targetMember = list.Members.FirstOrDefault(m => m.UserId == request.UserId)
            ?? throw new NotFoundException(nameof(Domain.Entities.ListMember), request.UserId);

        targetMember.Role = request.Role;

        await _listRepository.UpdateMemberAsync(targetMember, cancellationToken);
        await _listRepository.SaveChangesAsync(cancellationToken);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        return new MemberDto(targetMember.UserId, user.DisplayName, user.Email, targetMember.Role, targetMember.JoinedAt);
    }
}
