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
    private readonly Mock<IOptionRepository> _optionRepo = new();
    private readonly Mock<IOptionClaimRepository> _claimRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeService> _dateTime = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _partnerId = Guid.NewGuid();
    private readonly Guid _listId = Guid.NewGuid();
    private readonly DateTime _now = new(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc);

    private MarkItemPurchasedCommandHandler CreateHandler() => new(
        _itemRepo.Object,
        _listRepo.Object,
        _optionRepo.Object,
        _claimRepo.Object,
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

    private MarkItemPurchasedCommand Command(Guid itemId, params Guid[] purchaserIds)
        => new(itemId, purchaserIds.Length > 0 ? purchaserIds : new[] { _userId }, null);

    public MarkItemPurchasedCommandHandlerTests()
    {
        _listRepo.Setup(r => r.GetByIdWithMembersAsync(_listId, default)).ReturnsAsync(new TwoGather.Domain.Entities.List
        {
            Id = _listId,
            Members = new List<ListMember>
            {
                new() { Id = Guid.NewGuid(), ListId = _listId, UserId = _userId, Role = MemberRole.Owner, JoinedAt = _now },
                new() { Id = Guid.NewGuid(), ListId = _listId, UserId = _partnerId, Role = MemberRole.Editor, JoinedAt = _now }
            }
        });
        _currentUser.Setup(s => s.UserId).Returns(_userId);
        _dateTime.Setup(s => s.UtcNow).Returns(_now);
        // Default: items have no options (so no purchased option is required)
        _optionRepo.Setup(r => r.GetByItemIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new List<ItemOption>());
        // Default: no final option on any item
        _optionRepo.Setup(r => r.GetCurrentFinalOptionForItemAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((ItemOption?)null);
    }

    [Fact]
    public async Task Handle_OwnerMarksItem_SetsStatusAndNotifies()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));

        await CreateHandler().Handle(Command(item.Id), default);

        Assert.Equal(ItemStatus.Purchased, item.Status);
        Assert.Equal(_now, item.PurchasedAt);
        _itemRepo.Verify(r => r.UpdateAsync(item, default), Times.Once);
        _itemRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _notifications.Verify(n => n.ItemPurchasedAsync(_listId, item.Id, _now, default), Times.Once);
    }

    [Fact]
    public async Task Handle_EditorWithApprovedClaim_Succeeds()
    {
        var item = MakeItem();
        var finalOption = new ItemOption { Id = Guid.NewGuid(), ItemId = item.Id };
        var approvedClaim = new OptionClaim { Id = Guid.NewGuid(), OptionId = finalOption.Id, UserId = _userId, Percentage = 50, Status = ClaimStatus.Approved };

        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Editor));
        _optionRepo.Setup(r => r.GetCurrentFinalOptionForItemAsync(item.Id, default)).ReturnsAsync(finalOption);
        _claimRepo.Setup(r => r.GetByOptionIdAsync(finalOption.Id, default)).ReturnsAsync(new List<OptionClaim> { approvedClaim });

        await CreateHandler().Handle(Command(item.Id), default);

        Assert.Equal(ItemStatus.Purchased, item.Status);
    }

    [Fact]
    public async Task Handle_EditorWithoutClaim_ThrowsForbidden()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Editor));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(Command(item.Id), default));
    }

    [Fact]
    public async Task Handle_ViewerMarksItem_ThrowsForbidden()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Viewer));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(Command(item.Id), default));

        _notifications.Verify(n => n.ItemPurchasedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_NonMemberMarksItem_ThrowsForbidden()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync((ListMember?)null);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(Command(item.Id), default));
    }

    [Fact]
    public async Task Handle_AlreadyPurchased_ThrowsDomainException()
    {
        var item = MakeItem(ItemStatus.Purchased);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(Command(item.Id), default));

        _itemRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        _itemRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Item?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(Command(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_MultiplePurchasers_StoresEachOnce()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));
        IEnumerable<ItemPurchaser>? saved = null;
        _itemRepo.Setup(r => r.AddPurchasersAsync(It.IsAny<IEnumerable<ItemPurchaser>>(), default))
            .Callback<IEnumerable<ItemPurchaser>, CancellationToken>((p, _) => saved = p.ToList())
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(Command(item.Id, _userId, _partnerId, _partnerId), default);

        Assert.NotNull(saved);
        Assert.Equal(new[] { _userId, _partnerId }, saved!.Select(p => p.UserId).ToArray());
        Assert.All(saved!, p => Assert.Equal(item.Id, p.ItemId));
    }

    [Fact]
    public async Task Handle_PurchaserNotInList_ThrowsDomainException()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(Command(item.Id, Guid.NewGuid()), default));

        Assert.Equal(ItemStatus.Pending, item.Status);
        _itemRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public void Validator_EmptyPurchasers_Fails()
    {
        var result = new MarkItemPurchasedCommandValidator()
            .Validate(new MarkItemPurchasedCommand(Guid.NewGuid(), Array.Empty<Guid>(), null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Handle_ItemWithOptions_RequiresPurchasedOption()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));
        _optionRepo.Setup(r => r.GetByItemIdAsync(item.Id, default))
            .ReturnsAsync(new List<ItemOption> { new() { Id = Guid.NewGuid(), ItemId = item.Id, Title = "A" } });

        await Assert.ThrowsAsync<DomainException>(() => CreateHandler().Handle(Command(item.Id), default));
        Assert.Equal(ItemStatus.Pending, item.Status);
    }

    [Fact]
    public async Task Handle_OptionOfAnotherItem_IsRejected()
    {
        var item = MakeItem();
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));
        _optionRepo.Setup(r => r.GetByItemIdAsync(item.Id, default))
            .ReturnsAsync(new List<ItemOption> { new() { Id = Guid.NewGuid(), ItemId = item.Id, Title = "A" } });

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(new MarkItemPurchasedCommand(item.Id, new[] { _userId }, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_WithPurchasedOption_StoresIt()
    {
        var item = MakeItem();
        var option = new ItemOption { Id = Guid.NewGuid(), ItemId = item.Id, Title = "A" };
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(MakeMember(MemberRole.Owner));
        _optionRepo.Setup(r => r.GetByItemIdAsync(item.Id, default)).ReturnsAsync(new List<ItemOption> { option });

        await CreateHandler().Handle(new MarkItemPurchasedCommand(item.Id, new[] { _userId }, option.Id), default);

        Assert.Equal(ItemStatus.Purchased, item.Status);
        Assert.Equal(option.Id, item.PurchasedOptionId);
    }
}
