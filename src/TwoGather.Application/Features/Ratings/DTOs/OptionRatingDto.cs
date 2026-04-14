namespace TwoGather.Application.Features.Ratings.DTOs;

public record OptionRatingDto(
    decimal? AverageRating,
    int TotalRatings,
    int? CurrentUserScore
);
