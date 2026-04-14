using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Ratings.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Ratings.Commands.RateOption;

public class RateOptionCommandHandler : IRequestHandler<RateOptionCommand, OptionRatingDto>
{
    private readonly IOptionRatingRepository _ratingRepository;
    private readonly IOptionRepository _optionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public RateOptionCommandHandler(
        IOptionRatingRepository ratingRepository,
        IOptionRepository optionRepository,
        IItemRepository itemRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _ratingRepository = ratingRepository;
        _optionRepository = optionRepository;
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<OptionRatingDto> Handle(RateOptionCommand request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdWithItemAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        var existing = await _ratingRepository.GetByOptionAndUserAsync(request.OptionId, _currentUserService.UserId, cancellationToken);

        var now = _dateTimeService.UtcNow;

        if (existing is not null)
        {
            existing.Score = request.Score;
            existing.UpdatedAt = now;
            await _ratingRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var rating = new OptionRating
            {
                Id = Guid.NewGuid(),
                OptionId = request.OptionId,
                UserId = _currentUserService.UserId,
                Score = request.Score,
                CreatedAt = now
            };
            await _ratingRepository.AddAsync(rating, cancellationToken);
            await _ratingRepository.SaveChangesAsync(cancellationToken);
        }

        await _notificationService.OptionRatingUpdatedAsync(option.Item.ListId, request.OptionId, cancellationToken);

        return await BuildRatingDto(request.OptionId, _currentUserService.UserId, cancellationToken);
    }

    private async Task<OptionRatingDto> BuildRatingDto(Guid optionId, Guid userId, CancellationToken cancellationToken)
    {
        var all = await _optionRepository.GetRatingsForOptionAsync(optionId, cancellationToken);
        var total = all.Count;
        var average = total > 0 ? (decimal?)all.Average(r => r.Score) : null;
        var currentScore = all.FirstOrDefault(r => r.UserId == userId)?.Score;
        return new OptionRatingDto(average, total, currentScore);
    }
}
