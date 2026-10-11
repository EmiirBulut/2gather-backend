using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Items.Commands.DeleteItem;
using TwoGather.Application.Features.Items.Commands.UndoPurchase;
using TwoGather.Application.Features.Options.Commands.CreateOption;
using TwoGather.Application.Features.Ratings.Commands.RateOption;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Tests.Features.Items;

public class PurchasedItemLockTests
{
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IListRepository> _listRepo = new();
    private readonly Mock<IOptionRepository> _optionRepo = new();
    private readonly Mock<IOptionRatingRepository> _ratingRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeService> _dateTime = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _listId = Guid.NewGuid();
    private readonly DateTime _now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);

    public PurchasedItemLockTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(_userId);
        _dateTime.Setup(s => s.UtcNow).Returns(_now);
    }

    private Item MakeItem(ItemStatus status) => new()
    {
        Id = Guid.NewGuid(), ListId = _listId, CategoryId = Guid.NewGuid(), Name = "Koltuk",
        Status = status, PurchasedAt = status == ItemStatus.Purchased ? _now : null, CreatedAt = _now
    };

    private void SetRole(MemberRole role) =>
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default))
            .ReturnsAsync(new ListMember { ListId = _listId, UserId = _userId, Role = role });

    [Fact]
    public async Task RateOption_OnPurchasedItem_IsRejected()
    {
        var item = MakeItem(ItemStatus.Purchased);
        var option = new ItemOption { Id = Guid.NewGuid(), ItemId = item.Id, Title = "Kivik", Item = item };
        _optionRepo.Setup(r => r.GetByIdWithItemAsync(option.Id, default)).ReturnsAsync(option);
        SetRole(MemberRole.Editor);
        var handler = new RateOptionCommandHandler(_ratingRepo.Object, _optionRepo.Object, _itemRepo.Object,
            _listRepo.Object, _notifications.Object, _currentUser.Object, _dateTime.Object);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new RateOptionCommand(option.Id, 5), default));
        _ratingRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateOption_OnPurchasedItem_IsRejected()
    {
        var item = MakeItem(ItemStatus.Purchased);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        SetRole(MemberRole.Owner);
        var handler = new CreateOptionCommandHandler(_optionRepo.Object, _itemRepo.Object, _listRepo.Object,
            _notifications.Object, _currentUser.Object, _dateTime.Object);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new CreateOptionCommand(item.Id, "Yeni", 100, "TRY", null, null, null, null, null), default));
        _optionRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    private DeleteItemCommandHandler DeleteHandler() => new(_itemRepo.Object, _listRepo.Object,
        _notifications.Object, _currentUser.Object, _storage.Object, NullLogger<DeleteItemCommandHandler>.Instance);

    [Fact]
    public async Task DeleteItem_PurchasedByEditor_IsForbidden()
    {
        var item = MakeItem(ItemStatus.Purchased);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        SetRole(MemberRole.Editor);

        await Assert.ThrowsAsync<ForbiddenException>(() => DeleteHandler().Handle(new DeleteItemCommand(item.Id), default));
        _itemRepo.Verify(r => r.DeleteAsync(It.IsAny<Item>(), default), Times.Never);
    }

    [Fact]
    public async Task DeleteItem_PurchasedByOwner_IsAllowed()
    {
        var item = MakeItem(ItemStatus.Purchased);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        SetRole(MemberRole.Owner);

        await DeleteHandler().Handle(new DeleteItemCommand(item.Id), default);
        _itemRepo.Verify(r => r.DeleteAsync(item, default), Times.Once);
    }

    private UndoPurchaseCommandHandler UndoHandler() => new(_itemRepo.Object, _listRepo.Object,
        _notifications.Object, _currentUser.Object, _dateTime.Object, NullLogger<UndoPurchaseCommandHandler>.Instance);

    [Fact]
    public async Task UndoPurchase_Owner_ResetsItemAndPurchasers()
    {
        var item = MakeItem(ItemStatus.Purchased);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        SetRole(MemberRole.Owner);

        await UndoHandler().Handle(new UndoPurchaseCommand(item.Id), default);

        Assert.Equal(ItemStatus.Pending, item.Status);
        Assert.Null(item.PurchasedAt);
        _itemRepo.Verify(r => r.RemovePurchasersAsync(item.Id, default), Times.Once);
        _notifications.Verify(n => n.ItemPurchaseUndoneAsync(_listId, item.Id, default), Times.Once);
    }

    [Theory]
    [InlineData(MemberRole.Editor)]
    [InlineData(MemberRole.Viewer)]
    public async Task UndoPurchase_NonOwner_IsForbidden(MemberRole role)
    {
        var item = MakeItem(ItemStatus.Purchased);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        SetRole(role);

        await Assert.ThrowsAsync<ForbiddenException>(() => UndoHandler().Handle(new UndoPurchaseCommand(item.Id), default));
        Assert.Equal(ItemStatus.Purchased, item.Status);
    }

    [Fact]
    public async Task UndoPurchase_PendingItem_IsRejected()
    {
        var item = MakeItem(ItemStatus.Pending);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        SetRole(MemberRole.Owner);

        await Assert.ThrowsAsync<DomainException>(() => UndoHandler().Handle(new UndoPurchaseCommand(item.Id), default));
    }
}
