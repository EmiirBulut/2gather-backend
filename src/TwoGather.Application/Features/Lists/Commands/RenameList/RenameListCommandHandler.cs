using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Lists.Commands.RenameList;

public class RenameListCommandHandler : IRequestHandler<RenameListCommand>
{
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public RenameListCommandHandler(
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RenameListCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var member = await _listRepository.GetMemberAsync(list.Id, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        list.Name = request.Name.Trim();

        await _listRepository.UpdateAsync(list, cancellationToken);
        await _listRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.ListUpdatedAsync(list.Id, list.Name, cancellationToken);
    }
}
