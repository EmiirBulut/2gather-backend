using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Lists.Commands.CreateList;
using TwoGather.Application.Features.Lists.Commands.DeleteList;
using TwoGather.Application.Features.Lists.DTOs;
using TwoGather.Application.Features.Lists.Queries.GetListById;
using TwoGather.Application.Features.Lists.Queries.GetUserLists;
using TwoGather.Application.Features.Notifications.DTOs;
using TwoGather.Application.Features.Notifications.Queries.GetNotificationCount;

namespace TwoGather.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ListsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ListsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ListSummaryDto>>> GetUserLists(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserListsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ListDto>> CreateList(
        [FromBody] CreateListCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetListById), new { listId = result.Id }, result);
    }

    [HttpGet("{listId:guid}")]
    public async Task<ActionResult<ListDetailDto>> GetListById(Guid listId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetListByIdQuery(listId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{listId:guid}")]
    public async Task<IActionResult> DeleteList(Guid listId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteListCommand(listId), cancellationToken);
        return NoContent();
    }

    [HttpGet("{listId:guid}/notifications/count")]
    public async Task<ActionResult<NotificationCountDto>> GetNotificationCount(Guid listId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetNotificationCountQuery(listId), cancellationToken);
        return Ok(result);
    }
}
