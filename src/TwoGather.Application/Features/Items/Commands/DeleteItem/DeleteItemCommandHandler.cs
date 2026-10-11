using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Items.Commands.DeleteItem;

public class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DeleteItemCommandHandler> _logger;

    public DeleteItemCommandHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService,
        ILogger<DeleteItemCommandHandler> logger)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        var listId = item.ListId;
        var itemId = item.Id;
        var imageUrl = item.ImageUrl;

        await _itemRepository.DeleteAsync(item, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.ItemDeletedAsync(listId, itemId, cancellationToken);

        if (imageUrl is not null)
        {
            try
            {
                await _fileStorageService.DeleteImageAsync(imageUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                // The item is already gone; an orphaned image file is not worth failing the request
                _logger.LogWarning(ex, "Could not delete image for deleted item {ItemId}", itemId);
            }
        }
    }
}
