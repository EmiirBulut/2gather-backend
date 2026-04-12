using MediatR;

namespace TwoGather.Application.Features.Members.Commands.RemoveMember;

public record RemoveMemberCommand(Guid ListId, Guid UserId) : IRequest;
