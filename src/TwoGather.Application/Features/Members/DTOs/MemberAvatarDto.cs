namespace TwoGather.Application.Features.Members.DTOs;

public record MemberAvatarDto(
    Guid UserId,
    string DisplayName,
    string Initials
);
