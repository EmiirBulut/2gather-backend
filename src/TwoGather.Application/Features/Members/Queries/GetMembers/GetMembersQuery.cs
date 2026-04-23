using MediatR;
using TwoGather.Application.Features.Members.DTOs;

namespace TwoGather.Application.Features.Members.Queries.GetMembers;

public record GetMembersQuery(Guid ListId) : IRequest<IReadOnlyList<MemberDto>>;
