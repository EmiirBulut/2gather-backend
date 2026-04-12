using TwoGather.Domain.Enums;

namespace TwoGather.Domain.Entities;

public class ListMember
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public List List { get; set; } = null!;
    public User User { get; set; } = null!;
}
