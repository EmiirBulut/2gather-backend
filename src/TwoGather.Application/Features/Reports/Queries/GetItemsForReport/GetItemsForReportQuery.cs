using MediatR;
using TwoGather.Application.Features.Reports.DTOs;

namespace TwoGather.Application.Features.Reports.Queries.GetItemsForReport;

public record GetItemsForReportQuery(Guid ListId) : IRequest<IReadOnlyList<ReportItemDto>>;
