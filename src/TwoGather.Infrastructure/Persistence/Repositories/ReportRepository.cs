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

        var budgetUsagePercentage = estimatedTotal > 0
            ? Math.Round(totalSpent / estimatedTotal * 100, 1)
            : 0;
        var remainingBudget = estimatedTotal - totalSpent;

        var readinessPercentage = totalItems > 0
            ? Math.Round((decimal)purchasedCount / totalItems * 100, 1)
            : 0;
        var readinessLabel = readinessPercentage >= 80 ? "Tamamlandı"
            : readinessPercentage >= 50 ? "İyi Gidiyor"
            : readinessPercentage >= 20 ? "Devam Ediyor"
            : "Yeni Başladı";

        return new ListSummaryDto(totalItems, pendingCount, purchasedCount, totalSpent, estimatedTotal,
            budgetUsagePercentage, remainingBudget, readinessPercentage, readinessLabel);
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
            .Select(g =>
            {
                var total = g.Count();
                var purchased = g.Count(r => r.Status == ItemStatus.Purchased);
                return new CategoryReportDto(
                    g.Key.CategoryId,
                    g.Key.CategoryName,
                    total,
                    g.Count(r => r.Status == ItemStatus.Pending),
                    purchased,
                    g.Where(r => r.Status == ItemStatus.Purchased).Sum(r => r.SelectedOptionPrice),
                    total > 0 ? Math.Round((decimal)purchased / total * 100, 1) : 0
                );
            })
            .OrderBy(r => r.CategoryName)
            .ToList();
    }

    public async Task<IReadOnlyList<ReportItemDto>> GetItemsForReportAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Items.AsNoTracking()
            .Where(i => i.ListId == listId)
            .Select(i => new ReportItemDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                Name = i.Name,
                CategoryName = i.Category!.Name,
                Status = i.Status,
                SelectedOptionTitle = i.Options
                    .Where(o => o.IsSelected)
                    .Select(o => o.Title)
                    .FirstOrDefault(),
                EstimatedPrice = i.Options
                    .Where(o => o.IsSelected && o.Price.HasValue)
                    .Select(o => o.Price)
                    .FirstOrDefault(),
                Currency = i.Options
                    .Where(o => o.IsSelected)
                    .Select(o => o.Currency)
                    .FirstOrDefault()
            })
            .OrderBy(i => i.Status)
            .ThenBy(i => i.CategoryName)
            .ToListAsync(cancellationToken);
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
