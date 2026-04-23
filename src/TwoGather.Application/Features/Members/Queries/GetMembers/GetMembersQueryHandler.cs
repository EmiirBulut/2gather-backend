using MediatR;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Members.Queries.GetMembers;

public class GetMembersQueryHandler : IRequestHandler<GetMembersQuery, IReadOnlyList<MemberDto>>
{
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMembersQueryHandler(IListRepository listRepository, ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var member = await _listRepository.GetMemberAsync(request.ListId, currentUserId, cancellationToken);
        if (member is null) throw new ForbiddenException();

        return await _listRepository.GetMembersByListIdAsync(request.ListId, cancellationToken);
    }
}
