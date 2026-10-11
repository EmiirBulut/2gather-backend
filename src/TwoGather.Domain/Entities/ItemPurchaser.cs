namespace TwoGather.Domain.Entities;

public class ItemPurchaser
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
