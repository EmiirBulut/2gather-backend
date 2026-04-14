using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface IOptionRepository
{
    Task<ItemOption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ItemOption?> GetByIdWithItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OptionRating>> GetRatingsForOptionAsync(Guid optionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemOption>> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(ItemOption option, decimal? averageRating, int totalRatings, int? currentUserScore)>> GetByItemIdWithRatingsAsync(Guid itemId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<ItemOption?> GetCurrentFinalOptionForItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(ItemOption option, decimal? averageRating, int totalRatings, int? currentUserScore, int approvedClaimsTotal, List<Domain.Entities.OptionClaim> claims)>> GetByItemIdWithRatingsAndClaimsAsync(Guid itemId, Guid currentUserId, CancellationToken cancellationToken = default);
    Task AddAsync(ItemOption option, CancellationToken cancellationToken = default);
    Task UpdateAsync(ItemOption option, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<ItemOption> options, CancellationToken cancellationToken = default);
    Task DeleteAsync(ItemOption option, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
