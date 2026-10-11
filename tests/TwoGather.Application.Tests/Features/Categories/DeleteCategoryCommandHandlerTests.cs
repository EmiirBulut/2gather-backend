using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Categories.Commands.DeleteCategory;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Tests.Features.Categories;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IListRepository> _listRepo = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _listId = Guid.NewGuid();

    public DeleteCategoryCommandHandlerTests() => _currentUser.Setup(s => s.UserId).Returns(_userId);

    private DeleteCategoryCommandHandler CreateHandler() => new(_categoryRepo.Object, _itemRepo.Object, _listRepo.Object,
        _storage.Object, _notifications.Object, _currentUser.Object, NullLogger<DeleteCategoryCommandHandler>.Instance);

    private Category SetupCategory(Guid? listId)
    {
        var category = new Category { Id = Guid.NewGuid(), ListId = listId, Name = "Koltuk", RoomLabel = "Salon" };
        _categoryRepo.Setup(r => r.GetTrackedByIdAsync(category.Id, default)).ReturnsAsync(category);
        return category;
    }

    private void SetRole(MemberRole role) => _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default))
        .ReturnsAsync(new ListMember { ListId = _listId, UserId = _userId, Role = role });

    [Fact]
    public async Task Owner_DeletesCategoryItemsAndImages()
    {
        var category = SetupCategory(_listId);
        SetRole(MemberRole.Owner);
        var items = new List<Item>
        {
            new() { Id = Guid.NewGuid(), ListId = _listId, CategoryId = category.Id, Name = "Kivik", ImageUrl = "https://img/a.webp" },
            new() { Id = Guid.NewGuid(), ListId = _listId, CategoryId = category.Id, Name = "Bellona", Status = ItemStatus.Purchased },
        };
        _itemRepo.Setup(r => r.GetByCategoryIdAsync(category.Id, default)).ReturnsAsync(items);

        await CreateHandler().Handle(new DeleteCategoryCommand(category.Id), default);

        _itemRepo.Verify(r => r.DeleteAsync(It.IsAny<Item>(), default), Times.Exactly(2));
        _categoryRepo.Verify(r => r.DeleteAsync(category, default), Times.Once);
        _categoryRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _storage.Verify(s => s.DeleteImageAsync("https://img/a.webp", default), Times.Once);
        _notifications.Verify(n => n.CategoryDeletedAsync(_listId, category.Id, default), Times.Once);
    }

    [Theory]
    [InlineData(MemberRole.Editor)]
    [InlineData(MemberRole.Viewer)]
    public async Task NonOwner_IsForbidden(MemberRole role)
    {
        var category = SetupCategory(_listId);
        SetRole(role);

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateHandler().Handle(new DeleteCategoryCommand(category.Id), default));
        _categoryRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task GlobalCategory_IsForbidden()
    {
        var category = SetupCategory(null);

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateHandler().Handle(new DeleteCategoryCommand(category.Id), default));
        _categoryRepo.Verify(r => r.DeleteAsync(It.IsAny<Category>(), default), Times.Never);
    }
}
