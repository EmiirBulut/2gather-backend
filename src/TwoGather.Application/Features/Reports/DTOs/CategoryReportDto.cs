namespace TwoGather.Application.Features.Reports.DTOs;

public record CategoryReportDto(
    Guid CategoryId,
    string CategoryName,
    int TotalItems,
    int PendingCount,
    int PurchasedCount,
    decimal Spent
);
