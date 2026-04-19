using MediatR;

namespace TwoGather.Application.Features.Members.Commands.CancelInvite;

public record CancelInviteCommand(Guid ListId, Guid InviteId) : IRequest;
