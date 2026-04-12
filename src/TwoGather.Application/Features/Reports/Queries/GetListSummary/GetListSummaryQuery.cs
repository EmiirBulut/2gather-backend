using MediatR;
using TwoGather.Application.Features.Reports.DTOs;

namespace TwoGather.Application.Features.Reports.Queries.GetListSummary;

public record GetListSummaryQuery(Guid ListId) : IRequest<ListSummaryDto>;
