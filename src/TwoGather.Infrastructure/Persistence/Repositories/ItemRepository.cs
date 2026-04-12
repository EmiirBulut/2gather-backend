using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;

namespace TwoGather.Infrastructure.Persistence.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _dbContext;

    public ItemRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Item?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Items.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<Item?> GetByIdWithOptionsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Items.AsNoTracking()
            .Include(i => i.Options)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Item>> GetByListIdAsync(Guid listId, ItemStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Items.AsNoTracking()
            .Include(i => i.Options)
            .Where(i => i.ListId == listId);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        return await query.OrderBy(i => i.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Item item, CancellationToken cancellationToken = default)
        => await _dbContext.Items.AddAsync(item, cancellationToken);

    public Task UpdateAsync(Item item, CancellationToken cancellationToken = default)
    {
        _dbContext.Items.Update(item);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Item item, CancellationToken cancellationToken = default)
    {
        _dbContext.Items.Remove(item);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);
}
