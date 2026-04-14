using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Claims.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Claims.Queries.GetClaimsByOption;

public class GetClaimsByOptionQueryHandler : IRequestHandler<GetClaimsByOptionQuery, IReadOnlyList<ClaimDto>>
{
    private readonly IOptionClaimRepository _claimRepository;
    private readonly IOptionRepository _optionRepository;
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetClaimsByOptionQueryHandler(
        IOptionClaimRepository claimRepository,
        IOptionRepository optionRepository,
        IListRepository listRepository,
        ICurrentUserService currentUserService)
    {
        _claimRepository = claimRepository;
        _optionRepository = optionRepository;
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ClaimDto>> Handle(GetClaimsByOptionQuery request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdWithItemAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        var claims = await _claimRepository.GetByOptionIdAsync(request.OptionId, cancellationToken);

        return claims.Select(c => new ClaimDto(
            c.Id,
            c.UserId,
            c.User.DisplayName,
            c.Percentage,
            c.Status,
            c.CreatedAt
        )).ToList();
    }
}
