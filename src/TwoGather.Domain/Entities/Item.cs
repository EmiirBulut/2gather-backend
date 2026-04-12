using TwoGather.Domain.Enums;

namespace TwoGather.Domain.Entities;

public class Item
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ItemStatus Status { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List List { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ItemOption> Options { get; set; } = new List<ItemOption>();
}
