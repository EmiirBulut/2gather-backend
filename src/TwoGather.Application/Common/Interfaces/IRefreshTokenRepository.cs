using TwoGather.Domain.Entities;

namespace TwoGather.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
