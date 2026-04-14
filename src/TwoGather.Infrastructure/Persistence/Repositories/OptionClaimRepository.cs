using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;

namespace TwoGather.Infrastructure.Persistence.Repositories;

public class OptionClaimRepository : IOptionClaimRepository
{
    private readonly AppDbContext _dbContext;

    public OptionClaimRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OptionClaim?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _dbContext.OptionClaims
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<OptionClaim>> GetByOptionIdAsync(Guid optionId, CancellationToken ct)
        => await _dbContext.OptionClaims.AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.OptionId == optionId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> GetApprovedPercentageTotalAsync(Guid optionId, CancellationToken ct)
        => await _dbContext.OptionClaims
            .Where(c => c.OptionId == optionId && c.Status == ClaimStatus.Approved)
            .SumAsync(c => c.Percentage, ct);

    public async Task AddAsync(OptionClaim claim, CancellationToken ct)
        => await _dbContext.OptionClaims.AddAsync(claim, ct);

    public async Task SaveChangesAsync(CancellationToken ct)
        => await _dbContext.SaveChangesAsync(ct);
}
