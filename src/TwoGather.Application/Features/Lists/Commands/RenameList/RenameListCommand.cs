using MediatR;

namespace TwoGather.Application.Features.Lists.Commands.RenameList;

public record RenameListCommand(Guid ListId, string Name) : IRequest;
