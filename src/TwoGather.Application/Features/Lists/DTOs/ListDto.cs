namespace TwoGather.Application.Features.Lists.DTOs;

public record ListDto(
    Guid Id,
    string Name,
    Guid OwnerId,
    DateTime CreatedAt,
    int MemberCount
);
