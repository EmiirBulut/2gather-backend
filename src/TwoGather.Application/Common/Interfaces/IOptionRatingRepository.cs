using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface IOptionRatingRepository
{
    Task<OptionRating?> GetByOptionAndUserAsync(Guid optionId, Guid userId, CancellationToken ct);
    Task AddAsync(OptionRating rating, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
