using MediatR;

namespace TwoGather.Application.Features.Options.Commands.RemoveFinalDecision;

public record RemoveFinalDecisionCommand(Guid OptionId) : IRequest;
