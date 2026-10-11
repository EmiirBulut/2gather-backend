namespace TwoGather.Application.Features.Reports.DTOs;

public record ReportOptionDto(
    Guid Id,
    string Title,
    decimal? Price,
    string? Currency,
    bool IsFinal,
    bool IsSelected,
    bool IsPurchased
);
