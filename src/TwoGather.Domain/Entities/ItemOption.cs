namespace TwoGather.Domain.Entities;

public class ItemOption
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? Link { get; set; }
    public string? Notes { get; set; }
    public bool IsSelected { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public bool IsFinal { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public Guid? FinalizedBy { get; set; }

    public Item Item { get; set; } = null!;
    public ICollection<OptionRating> Ratings { get; set; } = new List<OptionRating>();
    public ICollection<OptionClaim> Claims { get; set; } = new List<OptionClaim>();
}
