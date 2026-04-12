using MediatR;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Members.Commands.InviteMember;

public record InviteMemberCommand(
    Guid ListId,
    string Email,
    MemberRole Role
) : IRequest<InviteDto>;
