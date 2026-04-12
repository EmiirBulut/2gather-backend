using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Items.Commands.MarkItemPurchased;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Tests.Features.Items;

public class MarkItemPurchasedCommandHandlerTests
{
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IListRepository> _listRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeService> _dateTime = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _listId = Guid.NewGuid();
    private readonly DateTime _now = new(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc);

    private MarkItemPurchasedCommandHandler CreateHandler() => new(
        _itemRepo.Object,
        _listRepo.Object,
        _notifications.Object,
        _currentUser.Object,
        _dateTime.Object,
        NullLogger<MarkItemPurchasedCommandHandler>.Instance);

    private Item MakeItem(ItemStatus status = ItemStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        ListId = _listId,
        CategoryId = Guid.NewGuid(),
        Name = "Test Item",
        Status = status,
        CreatedAt = _now
    };

    private ListMember MakeMember(MemberRole role) => new()
    {
        Id = Guid.NewGuid(),
        ListId = _listId,
        UserId = _userId,
        Role = role,
        JoinedAt = _now
    };

    public MarkItemPurchasedCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(_userId);
        _dateTime.Setup(s => s.UtcNow).Returns(_now);
    }

    [Fact]
    public async Task Handle_OwnerMarksItem_SetsStatusAndNotifies()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));

        await CreateHandler().Handle(new MarkItemPurchasedCommand(item.Id), default);

        Assert.Equal(ItemStatus.Purchased, item.Status);
        Assert.Equal(_now, item.PurchasedAt);
        _itemRepo.Verify(r => r.UpdateAsync(item, default), Times.Once);
        _itemRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _notifications.Verify(n => n.ItemPurchasedAsync(_listId, item.Id, _now, default), Times.Once);
    }

    [Fact]
    public async Task Handle_EditorMarksItem_Succeeds()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Editor));

        await CreateHandler().Handle(new MarkItemPurchasedCommand(item.Id), default);

        Assert.Equal(ItemStatus.Purchased, item.Status);
    }

    [Fact]
    public async Task Handle_ViewerMarksItem_ThrowsForbidden()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Viewer));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new MarkItemPurchasedCommand(item.Id), default));

        _notifications.Verify(n => n.ItemPurchasedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_NonMemberMarksItem_ThrowsForbidden()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync((ListMember?)null);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new MarkItemPurchasedCommand(item.Id), default));
    }

    [Fact]
    public async Task Handle_AlreadyPurchased_ThrowsDomainException()
    {
        var item = MakeItem(ItemStatus.Purchased);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(new MarkItemPurchasedCommand(item.Id), default));

        _itemRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        _itemRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Item?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new MarkItemPurchasedCommand(Guid.NewGuid()), default));
    }
}
