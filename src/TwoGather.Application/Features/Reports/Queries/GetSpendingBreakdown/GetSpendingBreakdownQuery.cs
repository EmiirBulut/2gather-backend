using MediatR;
using TwoGather.Application.Features.Reports.DTOs;

namespace TwoGather.Application.Features.Reports.Queries.GetSpendingBreakdown;

public record GetSpendingBreakdownQuery(Guid ListId) : IRequest<IReadOnlyList<SpendingBreakdownDto>>;
