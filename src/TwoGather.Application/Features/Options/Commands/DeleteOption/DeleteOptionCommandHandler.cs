using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Options.Commands.DeleteOption;

public class DeleteOptionCommandHandler : IRequestHandler<DeleteOptionCommand>
{
    private readonly IOptionRepository _optionRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteOptionCommandHandler(
        IOptionRepository optionRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _optionRepository = optionRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteOptionCommand request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdWithItemAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        var listId = option.Item.ListId;
        var itemId = option.Item.Id;
        var optionId = option.Id;

        await _optionRepository.DeleteAsync(option, cancellationToken);
        await _optionRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.OptionDeletedAsync(listId, itemId, optionId, cancellationToken);
    }
}
