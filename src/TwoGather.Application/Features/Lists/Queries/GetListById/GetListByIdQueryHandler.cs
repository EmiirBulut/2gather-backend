using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Lists.DTOs;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Lists.Queries.GetListById;

public class GetListByIdQueryHandler : IRequestHandler<GetListByIdQuery, ListDetailDto>
{
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetListByIdQueryHandler(IListRepository listRepository, ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ListDetailDto> Handle(GetListByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdWithMembersAndUsersAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var callerMember = list.Members.FirstOrDefault(m => m.UserId == _currentUserService.UserId);
        ListAuthorizationHelper.RequireRole(callerMember, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        var members = list.Members.Select(m => new MemberDto(
            m.UserId,
            m.User?.DisplayName ?? string.Empty,
            m.User?.Email ?? string.Empty,
            m.Role,
            m.JoinedAt
        )).ToList();

        return new ListDetailDto(list.Id, list.Name, list.OwnerId, list.CreatedAt, members);
    }
}
