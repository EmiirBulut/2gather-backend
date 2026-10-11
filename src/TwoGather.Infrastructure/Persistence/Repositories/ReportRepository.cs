using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Common.Pricing;
using TwoGather.Application.Features.Members.DTOs;
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

    private sealed record ItemPriceRow(
        Guid Id,
        string Name,
        string? ImageUrl,
        Guid CategoryId,
        string CategoryName,
        ItemStatus Status,
        DateTime? PurchasedAt,
        List<OptionPriceInfo> Options,
        List<MemberAvatarDto> Purchasers);

    // Prices are resolved in memory so every report uses the same EffectiveOption rule.
    private async Task<List<ItemPriceRow>> GetItemPriceRowsAsync(Guid listId, CancellationToken cancellationToken)
        => await _dbContext.Items.AsNoTracking()
            .Where(i => i.ListId == listId)
            .AsSplitQuery()
            .Select(i => new ItemPriceRow(
                i.Id,
                i.Name,
                i.ImageUrl,
                i.CategoryId,
                i.Category!.Name,
                i.Status,
                i.PurchasedAt,
                i.Options
                    .Select(o => new OptionPriceInfo(o.Title, o.Price, o.Currency, o.IsFinal, o.IsSelected))
                    .ToList(),
                i.Purchasers
                    .OrderBy(p => p.User.DisplayName)
                    .Select(p => new MemberAvatarDto(
                        p.UserId,
                        p.User.DisplayName,
                        p.User.DisplayName.Length >= 2
                            ? p.User.DisplayName.Substring(0, 2).ToUpper()
                            : p.User.DisplayName.ToUpper()))
                    .ToList()))
            .ToListAsync(cancellationToken);

    public async Task<ListSummaryDto> GetListSummaryAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var items = await GetItemPriceRowsAsync(listId, cancellationToken);

        var totalItems = items.Count;
        var pendingCount = items.Count(i => i.Status == ItemStatus.Pending);
        var purchasedCount = items.Count(i => i.Status == ItemStatus.Purchased);
        var totalSpent = items
            .Where(i => i.Status == ItemStatus.Purchased)
            .Sum(i => EffectiveOption.PriceOf(i.Options));
        var estimatedTotal = items.Sum(i => EffectiveOption.PriceOf(i.Options));

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
        var rows = await GetItemPriceRowsAsync(listId, cancellationToken);

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
                    g.Where(r => r.Status == ItemStatus.Purchased).Sum(r => EffectiveOption.PriceOf(r.Options)),
                    total > 0 ? Math.Round((decimal)purchased / total * 100, 1) : 0
                );
            })
            .OrderBy(r => r.CategoryName)
            .ToList();
    }

    public async Task<IReadOnlyList<ReportItemDto>> GetItemsForReportAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var rows = await GetItemPriceRowsAsync(listId, cancellationToken);

        return rows
            .Select(r =>
            {
                var effective = EffectiveOption.Pick(r.Options);
                return new ReportItemDto
                {
                    Id = r.Id,
                    ImageUrl = r.ImageUrl,
                    Name = r.Name,
                    CategoryId = r.CategoryId,
                    CategoryName = r.CategoryName,
                    Status = r.Status,
                    PurchasedAt = r.PurchasedAt,
                    OptionsCount = r.Options.Count,
                    Purchasers = r.Purchasers,
                    SelectedOptionTitle = effective?.Title,
                    EstimatedPrice = effective?.Price,
                    Currency = effective?.Currency
                };
            })
            .OrderBy(i => i.Status)
            .ThenBy(i => i.CategoryName)
            .ToList();
    }

    public async Task<IReadOnlyList<SpendingBreakdownDto>> GetSpendingBreakdownAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        var rows = await GetItemPriceRowsAsync(listId, cancellationToken);

        return rows
            .Where(r => r.Status == ItemStatus.Purchased && r.PurchasedAt.HasValue)
            .Select(r => new { Row = r, Effective = EffectiveOption.Pick(r.Options) })
            .Where(x => x.Effective is not null)
            .GroupBy(x => x.Row.CategoryName)
            .Select(g => new SpendingBreakdownDto(
                g.Key,
                g.Select(x => new SpendingItemDto(
                    x.Row.Id,
                    x.Row.Name,
                    x.Effective!.Title,
                    x.Effective.Price!.Value,
                    x.Effective.Currency,
                    x.Row.PurchasedAt!.Value
                )).ToList()
            ))
            .OrderBy(r => r.CategoryName)
            .ToList();
    }
}
