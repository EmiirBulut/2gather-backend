using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface IListInviteRepository
{
    Task<ListInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<ListInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ListInvite>> GetByListIdAsync(Guid listId, CancellationToken cancellationToken = default);
    Task AddAsync(ListInvite invite, CancellationToken cancellationToken = default);
    Task UpdateAcceptedAtAsync(ListInvite invite, CancellationToken cancellationToken = default);
    Task UpdateAsync(ListInvite invite, CancellationToken cancellationToken = default);
    Task DeleteAsync(ListInvite invite, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
