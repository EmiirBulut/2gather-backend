using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwoGather.Application.Features.Reports.DTOs;
using TwoGather.Application.Features.Reports.Queries.GetCategoryReport;
using TwoGather.Application.Features.Reports.Queries.GetListSummary;
using TwoGather.Application.Features.Reports.Queries.GetSpendingBreakdown;

namespace TwoGather.Api.Controllers;

[ApiController]
[Route("api/lists/{listId:guid}/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ListSummaryDto>> GetSummary(Guid listId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetListSummaryQuery(listId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-category")]
    public async Task<ActionResult<IReadOnlyList<CategoryReportDto>>> GetByCategory(Guid listId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoryReportQuery(listId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("spending")]
    public async Task<ActionResult<IReadOnlyList<SpendingBreakdownDto>>> GetSpending(Guid listId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSpendingBreakdownQuery(listId), cancellationToken);
        return Ok(result);
    }
}
