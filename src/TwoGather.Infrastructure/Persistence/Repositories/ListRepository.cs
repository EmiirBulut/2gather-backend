using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Repositories;

public class ListRepository : IListRepository
{
    private readonly AppDbContext _dbContext;

    public ListRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Lists.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<List?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Lists.AsNoTracking()
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<List?> GetByIdWithMembersAndUsersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Lists.AsNoTracking()
            .Include(l => l.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<List>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbContext.Lists.AsNoTracking()
            .Include(l => l.Members)
            .Where(l => l.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(List list, CancellationToken cancellationToken = default)
        => await _dbContext.Lists.AddAsync(list, cancellationToken);

    public Task DeleteAsync(List list, CancellationToken cancellationToken = default)
    {
        _dbContext.Lists.Remove(list);
        return Task.CompletedTask;
    }

    public async Task<ListMember?> GetMemberAsync(Guid listId, Guid userId, CancellationToken cancellationToken = default)
        => await _dbContext.ListMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ListId == listId && m.UserId == userId, cancellationToken);

    public async Task<ListMember?> GetOwnerAsync(Guid listId, CancellationToken cancellationToken = default)
        => await _dbContext.ListMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ListId == listId && m.Role == Domain.Enums.MemberRole.Owner, cancellationToken);

    public async Task AddMemberAsync(ListMember member, CancellationToken cancellationToken = default)
        => await _dbContext.ListMembers.AddAsync(member, cancellationToken);

    public Task RemoveMemberAsync(ListMember member, CancellationToken cancellationToken = default)
    {
        _dbContext.ListMembers.Remove(member);
        return Task.CompletedTask;
    }

    public Task UpdateMemberAsync(ListMember member, CancellationToken cancellationToken = default)
    {
        _dbContext.ListMembers.Update(member);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);
}
