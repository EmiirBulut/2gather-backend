using MediatR;
using TwoGather.Application.Features.Members.DTOs;

namespace TwoGather.Application.Features.Members.Queries.GetPendingInvites;

public record GetPendingInvitesQuery(Guid ListId) : IRequest<IReadOnlyList<PendingInviteDto>>;
