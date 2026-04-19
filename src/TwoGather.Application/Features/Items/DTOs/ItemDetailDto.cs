using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Items.DTOs;

public class ItemDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? PlanningNote { get; set; }
    public ItemStatus Status { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string RoomLabel { get; set; } = string.Empty;
    public List<ItemOptionDetailDto> Options { get; set; } = new();
}
