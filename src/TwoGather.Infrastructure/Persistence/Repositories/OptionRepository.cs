using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Repositories;

public class OptionRepository : IOptionRepository
{
    private readonly AppDbContext _dbContext;

    public OptionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ItemOption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.ItemOptions.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<ItemOption?> GetByIdWithItemAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.ItemOptions.AsNoTracking()
            .Include(o => o.Item)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OptionRating>> GetRatingsForOptionAsync(Guid optionId, CancellationToken cancellationToken = default)
        => await _dbContext.OptionRatings.AsNoTracking()
            .Where(r => r.OptionId == optionId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItemOption>> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default)
        => await _dbContext.ItemOptions.AsNoTracking()
            .Where(o => o.ItemId == itemId)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(ItemOption option, decimal? averageRating, int totalRatings, int? currentUserScore)>> GetByItemIdWithRatingsAsync(Guid itemId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.ItemOptions.AsNoTracking()
            .Where(o => o.ItemId == itemId)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new
            {
                Option = o,
                AverageRating = _dbContext.OptionRatings
                    .Where(r => r.OptionId == o.Id).Any()
                    ? (decimal?)_dbContext.OptionRatings
                        .Where(r => r.OptionId == o.Id).Average(r => r.Score)
                    : null,
                TotalRatings = _dbContext.OptionRatings.Count(r => r.OptionId == o.Id),
                CurrentUserScore = (int?)_dbContext.OptionRatings
                    .Where(r => r.OptionId == o.Id && r.UserId == currentUserId)
                    .Select(r => r.Score)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Option, r.AverageRating, r.TotalRatings, r.CurrentUserScore == 0 ? null : r.CurrentUserScore)).ToList();
    }

    public async Task<ItemOption?> GetCurrentFinalOptionForItemAsync(Guid itemId, CancellationToken cancellationToken = default)
        => await _dbContext.ItemOptions
            .FirstOrDefaultAsync(o => o.ItemId == itemId && o.IsFinal, cancellationToken);

    public async Task<IReadOnlyList<(ItemOption option, decimal? averageRating, int totalRatings, int? currentUserScore, int approvedClaimsTotal, List<Domain.Entities.OptionClaim> claims)>> GetByItemIdWithRatingsAndClaimsAsync(Guid itemId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var options = await _dbContext.ItemOptions.AsNoTracking()
            .Where(o => o.ItemId == itemId)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var optionIds = options.Select(o => o.Id).ToList();

        var ratings = await _dbContext.OptionRatings.AsNoTracking()
            .Where(r => optionIds.Contains(r.OptionId))
            .ToListAsync(cancellationToken);

        var claims = await _dbContext.OptionClaims.AsNoTracking()
            .Include(c => c.User)
            .Where(c => optionIds.Contains(c.OptionId))
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return options.Select(o =>
        {
            var optRatings = ratings.Where(r => r.OptionId == o.Id).ToList();
            var total = optRatings.Count;
            var avg = total > 0 ? (decimal?)optRatings.Average(r => r.Score) : null;
            var userScore = optRatings.FirstOrDefault(r => r.UserId == currentUserId)?.Score;
            var optClaims = claims.Where(c => c.OptionId == o.Id).ToList();
            var approvedTotal = optClaims.Where(c => c.Status == Domain.Enums.ClaimStatus.Approved).Sum(c => c.Percentage);
            return (o, avg, total, userScore == null ? (int?)null : userScore, approvedTotal, optClaims);
        }).ToList();
    }

    public async Task AddAsync(ItemOption option, CancellationToken cancellationToken = default)
        => await _dbContext.ItemOptions.AddAsync(option, cancellationToken);

    public Task UpdateAsync(ItemOption option, CancellationToken cancellationToken = default)
    {
        _dbContext.ItemOptions.Update(option);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<ItemOption> options, CancellationToken cancellationToken = default)
    {
        _dbContext.ItemOptions.UpdateRange(options);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ItemOption option, CancellationToken cancellationToken = default)
    {
        _dbContext.ItemOptions.Remove(option);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);
}
