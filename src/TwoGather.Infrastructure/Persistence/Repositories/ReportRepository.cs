using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Reports.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Infrastructure.Persistence.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _dbContext;

    public ReportRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ListSummaryDto> GetListSummaryAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Items.AsNoTracking()
            .Where(i => i.ListId == listId)
            .Select(i => new
            {
                i.Status,
                SelectedOptionPrice = i.Options
                    .Where(o => o.IsSelected && o.Price.HasValue)
                    .Select(o => o.Price!.Value)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var totalItems = items.Count;
        var pendingCount = items.Count(i => i.Status == ItemStatus.Pending);
        var purchasedCount = items.Count(i => i.Status == ItemStatus.Purchased);
        var totalSpent = items
            .Where(i => i.Status == ItemStatus.Purchased)
            .Sum(i => i.SelectedOptionPrice);
        var estimatedTotal = items.Sum(i => i.SelectedOptionPrice);

        return new ListSummaryDto(totalItems, pendingCount, purchasedCount, totalSpent, estimatedTotal);
    }

    public async Task<IReadOnlyList<CategoryReportDto>> GetCategoryReportAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Items.AsNoTracking()
            .Where(i => i.ListId == listId)
            .Select(i => new
            {
                i.CategoryId,
                CategoryName = i.Category!.Name,
                i.Status,
                SelectedOptionPrice = i.Options
                    .Where(o => o.IsSelected && o.Price.HasValue)
                    .Select(o => o.Price!.Value)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => new { r.CategoryId, r.CategoryName })
            .Select(g => new CategoryReportDto(
                g.Key.CategoryId,
                g.Key.CategoryName,
                g.Count(),
                g.Count(r => r.Status == ItemStatus.Pending),
                g.Count(r => r.Status == ItemStatus.Purchased),
                g.Where(r => r.Status == ItemStatus.Purchased).Sum(r => r.SelectedOptionPrice)
            ))
            .OrderBy(r => r.CategoryName)
            .ToList();
    }

    public async Task<IReadOnlyList<SpendingBreakdownDto>> GetSpendingBreakdownAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Items.AsNoTracking()
            .Where(i => i.ListId == listId && i.Status == ItemStatus.Purchased)
            .Select(i => new
            {
                CategoryName = i.Category!.Name,
                ItemId = i.Id,
                ItemName = i.Name,
                PurchasedAt = i.PurchasedAt!.Value,
                SelectedOption = i.Options
                    .Where(o => o.IsSelected && o.Price.HasValue)
                    .Select(o => new { o.Title, Price = o.Price!.Value, o.Currency })
                    .FirstOrDefault()
            })
            .Where(i => i.SelectedOption != null)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.CategoryName)
            .Select(g => new SpendingBreakdownDto(
                g.Key,
                g.Select(r => new SpendingItemDto(
                    r.ItemId,
                    r.ItemName,
                    r.SelectedOption!.Title,
                    r.SelectedOption.Price,
                    r.SelectedOption.Currency,
                    r.PurchasedAt
                )).ToList()
            ))
            .OrderBy(r => r.CategoryName)
            .ToList();
    }
}
