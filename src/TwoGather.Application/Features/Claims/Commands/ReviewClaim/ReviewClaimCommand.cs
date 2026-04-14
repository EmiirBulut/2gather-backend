using MediatR;
using TwoGather.Application.Features.Claims.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Claims.Commands.ReviewClaim;

public record ReviewClaimCommand(Guid ClaimId, ClaimStatus Decision) : IRequest<ClaimDto>;
