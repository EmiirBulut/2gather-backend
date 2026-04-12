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

    public async Task<IReadOnlyList<ItemOption>> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default)
        => await _dbContext.ItemOptions.AsNoTracking()
            .Where(o => o.ItemId == itemId)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ItemOption option, CancellationToken cancellationToken = default)
        => await _dbContext.ItemOptions.AddAsync(option, cancellationToken);

    public Task UpdateAsync(ItemOption option, CancellationToken cancellationToken = default)
    {
        _dbContext.ItemOptions.Update(option);
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
