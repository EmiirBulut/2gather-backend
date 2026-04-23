using Microsoft.EntityFrameworkCore;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Lists.DTOs;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;

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

    public async Task<IReadOnlyList<MemberDto>> GetMembersByListIdAsync(Guid listId, CancellationToken cancellationToken = default)
        => await _dbContext.ListMembers.AsNoTracking()
            .Where(m => m.ListId == listId)
            .Select(m => new MemberDto(m.UserId, m.User.DisplayName, m.User.Email, m.Role, m.JoinedAt))
            .ToListAsync(cancellationToken);

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

    public async Task<IReadOnlyList<ListSummaryDto>> GetUserListsSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Lists.AsNoTracking()
            .Where(l => l.Members.Any(m => m.UserId == userId))
            .Select(l => new ListSummaryDto
            {
                Id = l.Id,
                Name = l.Name,
                CreatedAt = l.CreatedAt,
                CurrentUserRole = l.Members.First(m => m.UserId == userId).Role,
                MemberCount = l.Members.Count(),
                TotalItemCount = l.Items.Count(),
                PurchasedItemCount = l.Items.Count(i => i.Status == ItemStatus.Purchased),
                PendingItemCount = l.Items.Count(i => i.Status == ItemStatus.Pending),
                CompletionPercentage = l.Items.Any()
                    ? Math.Round((decimal)l.Items.Count(i => i.Status == ItemStatus.Purchased) / l.Items.Count() * 100, 1)
                    : 0,
                Members = l.Members
                    .Take(3)
                    .Select(m => new MemberAvatarDto(
                        m.UserId,
                        m.User.DisplayName,
                        m.User.DisplayName.Length >= 2
                            ? m.User.DisplayName.Substring(0, 2).ToUpper()
                            : m.User.DisplayName.ToUpper()
                    )).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ListDetailDto?> GetListDetailAsync(Guid listId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.Lists.AsNoTracking()
            .Where(l => l.Id == listId)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.CreatedAt,
                CurrentUserRole = l.Members.First(m => m.UserId == currentUserId).Role,
                MemberCount = l.Members.Count(),
                TotalItemCount = l.Items.Count(),
                PurchasedItemCount = l.Items.Count(i => i.Status == ItemStatus.Purchased),
                TotalEstimated = l.Items
                    .SelectMany(i => i.Options.Where(o => o.IsSelected && o.Price.HasValue))
                    .Sum(o => (decimal?)o.Price) ?? 0,
                TotalSpent = l.Items
                    .Where(i => i.Status == ItemStatus.Purchased)
                    .SelectMany(i => i.Options.Where(o => o.IsSelected && o.Price.HasValue))
                    .Sum(o => (decimal?)o.Price) ?? 0,
                CategorySummaries = l.Items
                    .GroupBy(i => new { i.CategoryId, i.Category.Name, i.Category.RoomLabel })
                    .Select(g => new CategorySummaryDto
                    {
                        CategoryId = g.Key.CategoryId,
                        Name = g.Key.Name,
                        RoomLabel = g.Key.RoomLabel,
                        TotalItems = g.Count(),
                        PurchasedItems = g.Count(i => i.Status == ItemStatus.Purchased),
                        CompletionPercentage = g.Any()
                            ? Math.Round((decimal)g.Count(i => i.Status == ItemStatus.Purchased) / g.Count() * 100, 1)
                            : 0,
                        AssignedMembers = new List<MemberAvatarDto>()
                    }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (list is null) return null;

        var pendingClaims = await _dbContext.OptionClaims.AsNoTracking()
            .Where(c => c.Status == ClaimStatus.Pending && c.Option.Item.ListId == listId)
            .OrderBy(c => c.CreatedAt)
            .Take(5)
            .Select(c => new PendingClaimSummaryDto
            {
                ClaimId = c.Id,
                ItemId = c.Option.Item.Id,
                ItemName = c.Option.Item.Name,
                OptionTitle = c.Option.Title,
                ClaimantDisplayName = c.User.DisplayName,
                Percentage = c.Percentage,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ListDetailDto
        {
            Id = list.Id,
            Name = list.Name,
            CreatedAt = list.CreatedAt,
            CurrentUserRole = list.CurrentUserRole,
            MemberCount = list.MemberCount,
            CompletionPercentage = list.TotalItemCount > 0
                ? Math.Round((decimal)list.PurchasedItemCount / list.TotalItemCount * 100, 1)
                : 0,
            Financial = new FinancialSummaryDto
            {
                TotalEstimated = list.TotalEstimated,
                TotalSpent = list.TotalSpent,
                RemainingBudget = list.TotalEstimated - list.TotalSpent
            },
            PendingClaims = pendingClaims,
            CategorySummaries = list.CategorySummaries
        };
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);
}
