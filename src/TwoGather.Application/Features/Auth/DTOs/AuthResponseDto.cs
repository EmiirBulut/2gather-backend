namespace TwoGather.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string DisplayName
);
