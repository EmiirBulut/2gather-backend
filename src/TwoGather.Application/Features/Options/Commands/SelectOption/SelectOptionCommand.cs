using MediatR;
using TwoGather.Application.Features.Options.DTOs;

namespace TwoGather.Application.Features.Options.Commands.SelectOption;

public record SelectOptionCommand(Guid OptionId) : IRequest<ItemOptionDto>;
