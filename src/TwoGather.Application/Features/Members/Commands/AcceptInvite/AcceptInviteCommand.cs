using MediatR;
using TwoGather.Application.Features.Members.DTOs;

namespace TwoGather.Application.Features.Members.Commands.AcceptInvite;

public record AcceptInviteCommand(string Token) : IRequest<MemberDto>;
