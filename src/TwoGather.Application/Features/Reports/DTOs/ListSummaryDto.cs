namespace TwoGather.Application.Features.Reports.DTOs;

public record ListSummaryDto(
    int TotalItems,
    int PendingCount,
    int PurchasedCount,
    decimal TotalSpent,
    decimal EstimatedTotal
);
