namespace TwoGather.Domain.Entities;

public class List
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public ICollection<ListMember> Members { get; set; } = new List<ListMember>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
