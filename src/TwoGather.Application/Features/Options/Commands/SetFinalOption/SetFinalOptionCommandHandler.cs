using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Options.Commands.SetFinalOption;

public class SetFinalOptionCommandHandler : IRequestHandler<SetFinalOptionCommand>
{
    private readonly IOptionRepository _optionRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public SetFinalOptionCommandHandler(
        IOptionRepository optionRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _optionRepository = optionRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(SetFinalOptionCommand request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdWithItemAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        var existing = await _optionRepository.GetCurrentFinalOptionForItemAsync(option.Item.Id, cancellationToken);
        if (existing is not null && existing.Id != option.Id)
        {
            existing.IsFinal = false;
            existing.FinalizedAt = null;
            existing.FinalizedBy = null;
            await _optionRepository.UpdateAsync(existing, cancellationToken);
        }

        option.IsFinal = true;
        option.FinalizedAt = _dateTimeService.UtcNow;
        option.FinalizedBy = _currentUserService.UserId;

        await _optionRepository.UpdateAsync(option, cancellationToken);
        await _optionRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.OptionFinalizedAsync(option.Item.ListId, option.Item.Id, option.Id, cancellationToken);
    }
}
