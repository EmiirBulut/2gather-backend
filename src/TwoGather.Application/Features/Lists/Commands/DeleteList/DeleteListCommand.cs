using MediatR;

namespace TwoGather.Application.Features.Lists.Commands.DeleteList;

public record DeleteListCommand(Guid ListId) : IRequest;
