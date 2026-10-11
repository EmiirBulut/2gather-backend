using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Common.Interfaces;

public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Item?> GetByIdWithOptionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Item>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Item?> GetByIdWithCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Item item, int optionsCount, decimal? effectivePrice)>> GetByListIdAsync(Guid listId, ItemStatus? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemberAvatarDto>> GetPurchasersAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task AddAsync(Item item, CancellationToken cancellationToken = default);
    Task AddPurchasersAsync(IEnumerable<ItemPurchaser> purchasers, CancellationToken cancellationToken = default);
    Task RemovePurchasersAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Item item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Item item, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
