using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Claims.DTOs;

public record ClaimDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    int Percentage,
    ClaimStatus Status,
    DateTime CreatedAt
);
