using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Items.Commands.CreateItem;
using TwoGather.Application.Features.Items.Commands.DeleteItem;
using TwoGather.Application.Features.Items.Commands.MarkItemPurchased;
using TwoGather.Application.Features.Items.Commands.UpdateItem;
using TwoGather.Application.Features.Items.Commands.UploadItemImage;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Application.Features.Items.Queries.GetItemDetail;
using TwoGather.Application.Features.Items.Queries.GetItemsByList;
using TwoGather.Domain.Enums;

namespace TwoGather.Api.Controllers;

[ApiController]
[Authorize]
public class ItemsController : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

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
        var result = await _mediator.Send(new CreateItemCommand(listId, request.CategoryId, request.Name, request.ImageUrl, request.PlanningNote), cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/items/{itemId:guid}")]
    public async Task<ActionResult<ItemDetailDto>> GetItemDetail(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetItemDetailQuery(itemId), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("api/items/{itemId:guid}/status")]
    public async Task<IActionResult> MarkPurchased(
        Guid itemId,
        [FromBody] MarkPurchasedRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkItemPurchasedCommand(itemId, request.PurchaserUserIds), cancellationToken);
        return NoContent();
    }

    [HttpPut("api/items/{itemId:guid}")]
    public async Task<ActionResult<ItemDto>> UpdateItem(
        Guid itemId,
        [FromBody] UpdateItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateItemCommand(itemId, request.Name, request.CategoryId, request.ImageUrl, request.PlanningNote), cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/items/{itemId:guid}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes + 64 * 1024)]
    public async Task<ActionResult<UploadImageResponse>> UploadImage(
        Guid itemId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length > MaxImageBytes)
            return BadRequest("Image must be smaller than 5MB.");

        if (!AllowedImageTypes.Contains(file.ContentType))
            return BadRequest("Only JPEG, PNG and WebP images are allowed.");

        await using var stream = file.OpenReadStream();
        var imageUrl = await _mediator.Send(new UploadItemImageCommand(itemId, stream, file.ContentType), cancellationToken);
        return Ok(new UploadImageResponse(imageUrl));
    }

    [HttpDelete("api/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteItemCommand(itemId), cancellationToken);
        return NoContent();
    }
}

public record CreateItemRequest(Guid CategoryId, string Name, string? ImageUrl, string? PlanningNote);
public record UploadImageResponse(string ImageUrl);
public record MarkPurchasedRequest(IReadOnlyList<Guid> PurchaserUserIds);
public record UpdateItemRequest(string Name, Guid CategoryId, string? ImageUrl, string? PlanningNote);
