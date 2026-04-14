using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Entities;

namespace TwoGather.Infrastructure.Persistence.Repositories;

public class OptionRatingRepository : IOptionRatingRepository
{
    private readonly AppDbContext _dbContext;

    public OptionRatingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OptionRating?> GetByOptionAndUserAsync(Guid optionId, Guid userId, CancellationToken ct)
        => await _dbContext.OptionRatings
            .FirstOrDefaultAsync(r => r.OptionId == optionId && r.UserId == userId, ct);

    public async Task AddAsync(OptionRating rating, CancellationToken ct)
        => await _dbContext.OptionRatings.AddAsync(rating, ct);

    public async Task SaveChangesAsync(CancellationToken ct)
        => await _dbContext.SaveChangesAsync(ct);
}
