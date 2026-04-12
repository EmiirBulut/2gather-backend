using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Repositories;

public class ListInviteRepository : IListInviteRepository
{
    private readonly AppDbContext _dbContext;

    public ListInviteRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ListInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => await _dbContext.ListInvites.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

    public async Task AddAsync(ListInvite invite, CancellationToken cancellationToken = default)
        => await _dbContext.ListInvites.AddAsync(invite, cancellationToken);

    public Task UpdateAcceptedAtAsync(ListInvite invite, CancellationToken cancellationToken = default)
    {
        _dbContext.ListInvites.Update(invite);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);
}
