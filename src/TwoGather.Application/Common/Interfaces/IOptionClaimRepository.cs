using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface IOptionClaimRepository
{
    Task<OptionClaim?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<OptionClaim>> GetByOptionIdAsync(Guid optionId, CancellationToken ct);
    Task<int> GetApprovedPercentageTotalAsync(Guid optionId, CancellationToken ct);
    Task AddAsync(OptionClaim claim, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
