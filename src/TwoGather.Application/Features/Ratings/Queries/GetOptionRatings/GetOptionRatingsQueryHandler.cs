using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Ratings.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Ratings.Queries.GetOptionRatings;

public class GetOptionRatingsQueryHandler : IRequestHandler<GetOptionRatingsQuery, OptionRatingDto>
{
    private readonly IOptionRepository _optionRepository;
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOptionRatingsQueryHandler(
        IOptionRepository optionRepository,
        IListRepository listRepository,
        ICurrentUserService currentUserService)
    {
        _optionRepository = optionRepository;
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<OptionRatingDto> Handle(GetOptionRatingsQuery request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdWithItemAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        var ratings = await _optionRepository.GetRatingsForOptionAsync(request.OptionId, cancellationToken);
        var total = ratings.Count;
        var average = total > 0 ? (decimal?)ratings.Average(r => r.Score) : null;
        var currentScore = ratings.FirstOrDefault(r => r.UserId == _currentUserService.UserId)?.Score;

        return new OptionRatingDto(average, total, currentScore);
    }
}
