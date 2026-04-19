using TwoGather.Application.Features.Reports.DTOs;

namespace TwoGather.Application.Common.Interfaces;

public interface IReportRepository
{
    Task<ListSummaryDto> GetListSummaryAsync(Guid listId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryReportDto>> GetCategoryReportAsync(Guid listId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpendingBreakdownDto>> GetSpendingBreakdownAsync(Guid listId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportItemDto>> GetItemsForReportAsync(Guid listId, CancellationToken cancellationToken = default);
}
