using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Categories.Commands.CreateCustomCategory;
using TwoGather.Application.Features.Categories.DTOs;
using TwoGather.Application.Features.Categories.Queries.GetCategories;

namespace TwoGather.Api.Controllers;

[ApiController]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(
        [FromQuery] Guid listId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(listId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/lists/{listId:guid}/categories")]
    public async Task<ActionResult<CategoryDto>> CreateCustomCategory(
        Guid listId,
        [FromBody] CreateCustomCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCustomCategoryCommand(listId, request.Name, request.RoomLabel), cancellationToken);
        return Ok(result);
    }
}

public record CreateCustomCategoryRequest(string Name, string RoomLabel);
