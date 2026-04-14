using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface IListRepository
{
    Task<List?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List?> GetByIdWithMembersAndUsersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<List>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(List list, CancellationToken cancellationToken = default);
    Task DeleteAsync(List list, CancellationToken cancellationToken = default);
    Task<ListMember?> GetMemberAsync(Guid listId, Guid userId, CancellationToken cancellationToken = default);
    Task<ListMember?> GetOwnerAsync(Guid listId, CancellationToken cancellationToken = default);
    Task AddMemberAsync(ListMember member, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(ListMember member, CancellationToken cancellationToken = default);
    Task UpdateMemberAsync(ListMember member, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
