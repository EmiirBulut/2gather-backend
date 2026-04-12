namespace TwoGather.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public Guid? ListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomLabel { get; set; } = string.Empty;
    public bool IsSystem { get; set; }

    public List? List { get; set; }
}
