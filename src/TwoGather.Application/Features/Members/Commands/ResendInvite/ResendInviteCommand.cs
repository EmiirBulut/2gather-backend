using MediatR;

namespace TwoGather.Application.Features.Members.Commands.ResendInvite;

public record ResendInviteCommand(Guid ListId, Guid InviteId) : IRequest;
