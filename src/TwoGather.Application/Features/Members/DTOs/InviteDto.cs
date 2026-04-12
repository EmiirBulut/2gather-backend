namespace TwoGather.Application.Features.Members.DTOs;

public record InviteDto(
    Guid InviteId,
    string InvitedEmail,
    string Token,
    DateTime ExpiresAt
);
