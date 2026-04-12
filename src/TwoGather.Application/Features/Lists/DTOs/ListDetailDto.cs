using TwoGather.Application.Features.Members.DTOs;

namespace TwoGather.Application.Features.Lists.DTOs;

public record ListDetailDto(
    Guid Id,
    string Name,
    Guid OwnerId,
    DateTime CreatedAt,
    IReadOnlyList<MemberDto> Members
);
