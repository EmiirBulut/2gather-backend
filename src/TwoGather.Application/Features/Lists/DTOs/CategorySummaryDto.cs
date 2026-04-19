using TwoGather.Application.Features.Members.DTOs;

namespace TwoGather.Application.Features.Lists.DTOs;

public class CategorySummaryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomLabel { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int PurchasedItems { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<MemberAvatarDto> AssignedMembers { get; set; } = new();
}
