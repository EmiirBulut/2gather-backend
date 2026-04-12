using MediatR;

namespace TwoGather.Application.Features.Options.Commands.DeleteOption;

public record DeleteOptionCommand(Guid OptionId) : IRequest;
