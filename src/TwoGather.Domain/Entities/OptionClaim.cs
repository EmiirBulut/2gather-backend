using TwoGather.Domain.Enums;

namespace TwoGather.Domain.Entities;

public class OptionClaim
{
    public Guid Id { get; set; }
    public Guid OptionId { get; set; }
    public ItemOption Option { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int Percentage { get; set; }
    public ClaimStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
}
