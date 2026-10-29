using MediatR;
using Microsoft.Extensions.Logging;
using TwoGather.Application.Common.Helpers;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Features.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IListRepository _listRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IItemRepository itemRepository,
        IListRepository listRepository,
        IFileStorageService fileStorageService,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        ILogger<DeleteCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _itemRepository = itemRepository;
        _listRepository = listRepository;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetTrackedByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (category.ListId is not { } listId)
            throw new ForbiddenException("Global system categories cannot be deleted.");

        // Deleting a category deletes its items, including purchased ones, so it is owner-only
        var member = await _listRepository.GetMemberAsync(listId, _currentUserService.UserId, cancellationToken);
        ListAuthorizationHelper.RequireRole(member, MemberRole.Owner);

        var items = await _itemRepository.GetByCategoryIdAsync(category.Id, cancellationToken);
        foreach (var item in items)
            await _itemRepository.DeleteAsync(item, cancellationToken);
        await _categoryRepository.DeleteAsync(category, cancellationToken);

        // Options, ratings, claims and purchasers cascade from their item in the database
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category {CategoryId} and {ItemCount} items deleted from list {ListId}",
            category.Id, items.Count, listId);

        await _notificationService.CategoryDeletedAsync(listId, category.Id, cancellationToken);

        foreach (var imageUrl in items.Select(i => i.ImageUrl).OfType<string>())
        {
            try
            {
                await _fileStorageService.DeleteImageAsync(imageUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                // The data is already deleted; an orphaned image file is not worth failing the request
                _logger.LogWarning(ex, "Could not delete an item image while deleting category {CategoryId}", category.Id);
            }
        }
    }
}
