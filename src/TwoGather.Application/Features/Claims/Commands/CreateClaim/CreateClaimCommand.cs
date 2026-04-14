using MediatR;
using TwoGather.Application.Features.Claims.DTOs;

namespace TwoGather.Application.Features.Claims.Commands.CreateClaim;

public record CreateClaimCommand(Guid OptionId, int Percentage) : IRequest<ClaimDto>;
