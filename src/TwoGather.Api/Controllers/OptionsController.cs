using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Options.Commands.CreateOption;
using TwoGather.Application.Features.Options.Commands.DeleteOption;
using TwoGather.Application.Features.Options.Commands.SelectOption;
using TwoGather.Application.Features.Options.Commands.UpdateOption;
using TwoGather.Application.Features.Options.DTOs;
using TwoGather.Application.Features.Options.Queries.GetOptionsByItem;

namespace TwoGather.Api.Controllers;

[ApiController]
[Authorize]
public class OptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/items/{itemId:guid}/options")]
    public async Task<ActionResult<IReadOnlyList<ItemOptionDto>>> GetOptions(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOptionsByItemQuery(itemId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/items/{itemId:guid}/options")]
    public async Task<ActionResult<ItemOptionDto>> CreateOption(
        Guid itemId,
        [FromBody] CreateOptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateOptionCommand(itemId, request.Title, request.Price, request.Currency, request.Link, request.Notes),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("api/options/{optionId:guid}")]
    public async Task<ActionResult<ItemOptionDto>> UpdateOption(
        Guid optionId,
        [FromBody] UpdateOptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateOptionCommand(optionId, request.Title, request.Price, request.Currency, request.Link, request.Notes),
            cancellationToken);
        return Ok(result);
    }

    [HttpPatch("api/options/{optionId:guid}/select")]
    public async Task<ActionResult<ItemOptionDto>> SelectOption(
        Guid optionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SelectOptionCommand(optionId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/options/{optionId:guid}")]
    public async Task<IActionResult> DeleteOption(Guid optionId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteOptionCommand(optionId), cancellationToken);
        return NoContent();
    }
}

public record CreateOptionRequest(string Title, decimal? Price, string? Currency, string? Link, string? Notes);
public record UpdateOptionRequest(string Title, decimal? Price, string? Currency, string? Link, string? Notes);
