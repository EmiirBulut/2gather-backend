using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Options.DTOs;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Options.Commands.UpdateOption;

public class UpdateOptionCommandHandler : IRequestHandler<UpdateOptionCommand, ItemOptionDto>
{
    private readonly IOptionRepository _optionRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateOptionCommandHandler(
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

    public async Task<ItemOptionDto> Handle(UpdateOptionCommand request, CancellationToken cancellationToken)
    {
        var option = await _optionRepository.GetByIdWithItemAsync(request.OptionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ItemOption), request.OptionId);

        var member = await _listRepository.GetMemberAsync(option.Item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        if (option.IsFinal && member!.Role != MemberRole.Owner)
            throw new ForbiddenException();

        option.Title = request.Title;
        option.Price = request.Price;
        option.Currency = request.Currency;
        option.Link = request.Link;
        option.Notes = request.Notes;
        option.Brand = request.Brand;
        option.Model = request.Model;
        option.Color = request.Color;

        await _optionRepository.UpdateAsync(option, cancellationToken);
        await _optionRepository.SaveChangesAsync(cancellationToken);

        var dto = new ItemOptionDto(option.Id, option.ItemId, option.Title, option.Price, option.Currency, option.Link, option.Notes, option.IsSelected, option.CreatedAt, option.Brand, option.Model, option.Color, null, 0, null, option.IsFinal, option.FinalizedAt);

        await _notificationService.OptionUpdatedAsync(option.Item.ListId, option.Item.Id, dto, cancellationToken);

        return dto;
    }
}
