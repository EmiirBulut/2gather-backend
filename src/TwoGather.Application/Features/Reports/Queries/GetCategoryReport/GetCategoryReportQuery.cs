using MediatR;
using TwoGather.Application.Features.Reports.DTOs;

namespace TwoGather.Application.Features.Reports.Queries.GetCategoryReport;

public record GetCategoryReportQuery(Guid ListId) : IRequest<IReadOnlyList<CategoryReportDto>>;
