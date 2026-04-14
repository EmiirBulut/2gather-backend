using TwoGather.Application.Features.Claims.DTOs;

namespace TwoGather.Application.Features.Options.DTOs;

public record ItemOptionDto(
    Guid Id,
    Guid ItemId,
    string Title,
    decimal? Price,
    string? Currency,
    string? Link,
    string? Notes,
    bool IsSelected,
    DateTime CreatedAt,
    string? Brand,
    string? Model,
    string? Color,
    decimal? AverageRating,
    int TotalRatings,
    int? CurrentUserScore,
    bool IsFinal,
    DateTime? FinalizedAt,
    int ApprovedClaimsTotal,
    int RemainingClaimPercentage,
    List<ClaimDto> Claims
);
