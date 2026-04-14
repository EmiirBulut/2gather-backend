using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Options.Commands.RemoveFinalDecision;

public class RemoveFinalDecisionCommandHandler : IRequestHandler<RemoveFinalDecisionCommand>
{
    private readonly IOptionRepository _optionRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFinalDecisionCommandHandler(
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

    public async Task Handle(RemoveFinalDecisionCommand request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdWithItemAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        if (!option.IsFinal)
            throw new DomainException("Bu seçeneğe nihai karar verilmemiş.");

        option.IsFinal = false;
        option.FinalizedAt = null;
        option.FinalizedBy = null;

        await _optionRepository.UpdateAsync(option, cancellationToken);
        await _optionRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.OptionFinalRemovedAsync(option.Item.ListId, option.Item.Id, option.Id, cancellationToken);
    }
}
