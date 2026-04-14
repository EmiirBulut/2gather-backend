using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Ratings.Commands.RateOption;
using TwoGather.Application.Features.Ratings.DTOs;
using TwoGather.Application.Features.Ratings.Queries.GetOptionRatings;

namespace TwoGather.Api.Controllers;

[ApiController]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RatingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("api/options/{optionId:guid}/ratings")]
    public async Task<ActionResult<OptionRatingDto>> RateOption(
        Guid optionId,
        [FromBody] RateOptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RateOptionCommand(optionId, request.Score), cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/options/{optionId:guid}/ratings")]
    public async Task<ActionResult<OptionRatingDto>> GetRatings(
        Guid optionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOptionRatingsQuery(optionId), cancellationToken);
        return Ok(result);
    }
}

public record RateOptionRequest(int Score);
