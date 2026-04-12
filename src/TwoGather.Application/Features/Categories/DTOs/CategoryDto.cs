namespace TwoGather.Application.Features.Categories.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string RoomLabel,
    bool IsSystem,
    Guid? ListId
);
