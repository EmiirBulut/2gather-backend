using MediatR;
using TwoGather.Application.Features.Claims.DTOs;

namespace TwoGather.Application.Features.Claims.Queries.GetClaimsByOption;

public record GetClaimsByOptionQuery(Guid OptionId) : IRequest<IReadOnlyList<ClaimDto>>;
