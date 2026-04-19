using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Reports.DTOs;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Features.Reports.Queries.GetItemsForReport;

public class GetItemsForReportQueryHandler : IRequestHandler<GetItemsForReportQuery, IReadOnlyList<ReportItemDto>>
{
    private readonly IReportRepository _reportRepository;
    private readonly IListRepository _listRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetItemsForReportQueryHandler(
        IReportRepository reportRepository,
        IListRepository listRepository,
        ICurrentUserService currentUserService)
    {
        _reportRepository = reportRepository;
        _listRepository = listRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ReportItemDto>> Handle(GetItemsForReportQuery request, CancellationToken cancellationToken)
    {
        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor, MemberRole.Viewer);

        return await _reportRepository.GetItemsForReportAsync(request.ListId, cancellationToken);
    }
}
