using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Lists.DTOs;

public class ListSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MemberRole CurrentUserRole { get; set; }
    public int MemberCount { get; set; }
    public int TotalItemCount { get; set; }
    public int PurchasedItemCount { get; set; }
    public int PendingItemCount { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<MemberAvatarDto> Members { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
