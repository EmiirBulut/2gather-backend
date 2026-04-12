namespace TwoGather.Application.Features.Reports.DTOs;

public record SpendingBreakdownDto(
    string CategoryName,
    IReadOnlyList<SpendingItemDto> Items
);

public record SpendingItemDto(
    Guid ItemId,
    string ItemName,
    string OptionTitle,
    decimal Price,
    string? Currency,
    DateTime PurchasedAt
);
