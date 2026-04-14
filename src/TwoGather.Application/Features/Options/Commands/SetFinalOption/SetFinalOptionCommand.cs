using MediatR;

namespace TwoGather.Application.Features.Options.Commands.SetFinalOption;

public record SetFinalOptionCommand(Guid OptionId) : IRequest;
