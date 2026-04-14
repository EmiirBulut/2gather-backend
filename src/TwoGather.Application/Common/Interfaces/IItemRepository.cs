using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Common.Interfaces;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Item?> GetByIdWithOptionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Item item, int optionsCount)>> GetByListIdAsync(Guid listId, ItemStatus? status, CancellationToken cancellationToken = default);
    Task AddAsync(Item item, CancellationToken cancellationToken = default);
    Task UpdateAsync(Item item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Item item, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
