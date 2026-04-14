using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Options.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Options.Commands.CreateOption;

public class CreateOptionCommandHandler : IRequestHandler<CreateOptionCommand, ItemOptionDto>
{
    private readonly IOptionRepository _optionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public CreateOptionCommandHandler(
        IOptionRepository optionRepository,
        IItemRepository itemRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _optionRepository = optionRepository;
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ItemOptionDto> Handle(CreateOptionCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        var option = new ItemOption
        {
            Id = Guid.NewGuid(),
            ItemId = request.ItemId,
            Title = request.Title,
            Price = request.Price,
            Currency = request.Currency,
            Link = request.Link,
            Notes = request.Notes,
            IsSelected = false,
            CreatedAt = _dateTimeService.UtcNow,
            Brand = request.Brand,
            Model = request.Model,
            Color = request.Color
        };

        await _optionRepository.AddAsync(option, cancellationToken);
        await _optionRepository.SaveChangesAsync(cancellationToken);

        var dto = new ItemOptionDto(option.Id, option.ItemId, option.Title, option.Price, option.Currency, option.Link, option.Notes, option.IsSelected, option.CreatedAt, option.Brand, option.Model, option.Color, null, 0, null);

        await _notificationService.OptionAddedAsync(item.ListId, item.Id, dto, cancellationToken);

        return dto;
    }
}
