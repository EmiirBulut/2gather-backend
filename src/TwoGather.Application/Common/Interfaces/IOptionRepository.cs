using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface IOptionRepository
{
    Task<ItemOption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemOption>> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task AddAsync(ItemOption option, CancellationToken cancellationToken = default);
    Task UpdateAsync(ItemOption option, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<ItemOption> options, CancellationToken cancellationToken = default);
    Task DeleteAsync(ItemOption option, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
