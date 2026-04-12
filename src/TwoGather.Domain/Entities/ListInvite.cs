using TwoGather.Domain.Enums;

namespace TwoGather.Domain.Entities;

public class ListInvite
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public MemberRole Role { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List List { get; set; } = null!;
}
