using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Items.DTOs;

public record ItemDto(
    Guid Id,
    Guid ListId,
    Guid CategoryId,
    string Name,
    ItemStatus Status,
    DateTime? PurchasedAt,
    DateTime CreatedAt,
    int OptionsCount
);
