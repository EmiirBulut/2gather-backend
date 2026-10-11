using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Items.Commands.UploadItemImage;

public class UploadItemImageCommandHandler : IRequestHandler<UploadItemImageCommand, string>
{
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<UploadItemImageCommandHandler> _logger;

    public UploadItemImageCommandHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        IFileStorageService fileStorageService,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ILogger<UploadItemImageCommandHandler> logger)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<string> Handle(UploadItemImageCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        var imageUrl = await _fileStorageService.UploadImageAsync(
            request.ImageStream, request.ContentType, $"items/{item.Id}", cancellationToken);

        var previousImageUrl = item.ImageUrl;
        item.ImageUrl = imageUrl;
        item.UpdatedAt = _dateTimeService.UtcNow;

        await _itemRepository.UpdateAsync(item, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.ItemImageUpdatedAsync(item.ListId, item.Id, imageUrl, cancellationToken);

        if (previousImageUrl is not null)
        {
            try
            {
                await _fileStorageService.DeleteImageAsync(previousImageUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                // The new image is already saved; an orphaned old file is not worth failing the request
                _logger.LogWarning(ex, "Could not delete previous image for item {ItemId}", item.Id);
            }
        }

        return imageUrl;
    }
}
