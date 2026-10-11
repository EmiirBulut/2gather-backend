using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Reports.DTOs;

public class ReportItemDto
{
    public Guid Id { get; set; }
    public string? ImageUrl { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SelectedOptionTitle { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string RoomLabel { get; set; } = string.Empty;
    public decimal? EstimatedPrice { get; set; }
    public string? Currency { get; set; }
    public ItemStatus Status { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public int OptionsCount { get; set; }
    public List<MemberAvatarDto> Purchasers { get; set; } = new();
    // Alternatives of the item; most expensive first
    public List<ReportOptionDto> Options { get; set; } = new();
}
