using MediatR;
using TwoGather.Application.Features.Options.DTOs;

namespace TwoGather.Application.Features.Options.Commands.CreateOption;

public record CreateOptionCommand(
    Guid ItemId,
    string Title,
    decimal? Price,
    string? Currency,
    string? Link,
    string? Notes
) : IRequest<ItemOptionDto>;
