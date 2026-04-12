using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface IListInviteRepository
{
    Task<ListInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(ListInvite invite, CancellationToken cancellationToken = default);
    Task UpdateAcceptedAtAsync(ListInvite invite, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
