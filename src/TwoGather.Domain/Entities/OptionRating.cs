namespace TwoGather.Domain.Entities;

public class OptionRating
{
    public Guid Id { get; set; }
    public Guid OptionId { get; set; }
    public ItemOption Option { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
