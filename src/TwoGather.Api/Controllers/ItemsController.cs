using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Items.Commands.CreateItem;
using TwoGather.Application.Features.Items.Commands.DeleteItem;
using TwoGather.Application.Features.Items.Commands.MarkItemPurchased;
using TwoGather.Application.Features.Items.Commands.UpdateItem;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Application.Features.Items.Queries.GetItemsByList;
using TwoGather.Domain.Enums;

namespace TwoGather.Api.Controllers;

[ApiController]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/lists/{listId:guid}/items")]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> GetItems(
        Guid listId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        ItemStatus? itemStatus = status?.ToLowerInvariant() switch
        {
            "pending"   => ItemStatus.Pending,
            "purchased" => ItemStatus.Purchased,
            _           => null
        };

        var result = await _mediator.Send(new GetItemsByListQuery(listId, itemStatus), cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/lists/{listId:guid}/items")]
    public async Task<ActionResult<ItemDto>> CreateItem(
        Guid listId,
        [FromBody] CreateItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateItemCommand(listId, request.CategoryId, request.Name), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("api/items/{itemId:guid}/status")]
    public async Task<IActionResult> MarkPurchased(Guid itemId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkItemPurchasedCommand(itemId), cancellationToken);
        return NoContent();
    }

    [HttpPut("api/items/{itemId:guid}")]
    public async Task<ActionResult<ItemDto>> UpdateItem(
        Guid itemId,
        [FromBody] UpdateItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateItemCommand(itemId, request.Name, request.CategoryId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteItemCommand(itemId), cancellationToken);
        return NoContent();
    }
}

public record CreateItemRequest(Guid CategoryId, string Name);
public record UpdateItemRequest(string Name, Guid CategoryId);
