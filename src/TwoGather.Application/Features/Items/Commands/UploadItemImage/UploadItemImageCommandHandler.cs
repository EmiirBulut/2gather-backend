using MediatR;
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

    public UploadItemImageCommandHandler(
        IItemRepository itemRepository,
        IListRepository listRepository,
        IFileStorageService fileStorageService,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<string> Handle(UploadItemImageCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Item), request.ItemId);

        var member = await _listRepository.GetMemberAsync(item.ListId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner, MemberRole.Editor);

        var imageUrl = await _fileStorageService.UploadImageAsync(request.ImageStream, request.FileName, cancellationToken);

        item.ImageUrl = imageUrl;
        item.UpdatedAt = _dateTimeService.UtcNow;

        await _itemRepository.UpdateAsync(item, cancellationToken);
        await _itemRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.ItemImageUpdatedAsync(item.ListId, item.Id, imageUrl, cancellationToken);

        return imageUrl;
    }
}
