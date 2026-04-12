using MediatR;
using TwoGather.Application.Features.Members.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Members.Commands.UpdateMemberRole;

public record UpdateMemberRoleCommand(Guid ListId, Guid UserId, MemberRole Role) : IRequest<MemberDto>;
