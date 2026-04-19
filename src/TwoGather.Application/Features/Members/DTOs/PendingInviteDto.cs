using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Members.DTOs;

public class PendingInviteDto
{
    public Guid InviteId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public MemberRole Role { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExpired { get; set; }
}
