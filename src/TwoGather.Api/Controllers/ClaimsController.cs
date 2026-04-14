using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Claims.Commands.CreateClaim;
using TwoGather.Application.Features.Claims.Commands.ReviewClaim;
using TwoGather.Application.Features.Claims.DTOs;
using TwoGather.Application.Features.Claims.Queries.GetClaimsByOption;
using TwoGather.Domain.Enums;

namespace TwoGather.Api.Controllers;

[ApiController]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClaimsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("api/options/{optionId:guid}/claims")]
    public async Task<ActionResult<ClaimDto>> CreateClaim(
        Guid optionId,
        [FromBody] CreateClaimRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateClaimCommand(optionId, request.Percentage), cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/options/{optionId:guid}/claims")]
    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> GetClaims(
        Guid optionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClaimsByOptionQuery(optionId), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("api/claims/{claimId:guid}/review")]
    public async Task<ActionResult<ClaimDto>> ReviewClaim(
        Guid claimId,
        [FromBody] ReviewClaimRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ReviewClaimCommand(claimId, request.Decision), cancellationToken);
        return Ok(result);
    }
}

public record CreateClaimRequest(int Percentage);
public record ReviewClaimRequest(ClaimStatus Decision);
