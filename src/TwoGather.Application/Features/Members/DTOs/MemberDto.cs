using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Members.DTOs;

public record MemberDto(
    Guid UserId,
    string DisplayName,
    string Email,
    MemberRole Role,
    DateTime JoinedAt
);
