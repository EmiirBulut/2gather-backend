using MediatR;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Items.DTOs;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Items.Commands.CreateItem;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDto>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public CreateItemCommandHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<ItemDto> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var list = await _listRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.List), request.ListId);

        var member = await _listRepository.GetMemberAsync(request.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        var now = _dateTimeService.UtcNow;
        var item = new Item
        {
            Id = Guid.NewGuid(),
            ListId = request.ListId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Status = ItemStatus.Pending,
            ImageUrl = request.ImageUrl,
            PlanningNote = request.PlanningNote,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _itemRepository.AddAsync(item, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        var dto = new ItemDto(item.Id, item.ListId, item.CategoryId, item.Name, item.Status, item.PurchasedAt, item.ImageUrl, item.PlanningNote, item.CreatedAt, item.UpdatedAt, 0);

        await _notificationService.ItemAddedAsync(request.ListId, dto, cancellationToken);

        return dto;
    }
}
