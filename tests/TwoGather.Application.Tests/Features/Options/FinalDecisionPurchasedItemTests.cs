using Moq;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Options.Commands.RemoveFinalDecision;
using TwoGather.Application.Features.Options.Commands.SetFinalOption;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Tests.Features.Options;

public class FinalDecisionPurchasedItemTests
{
    private readonly Mock<IOptionRepository> _optionRepo = new();
    private readonly Mock<IListRepository> _listRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeService> _dateTime = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _listId = Guid.NewGuid();
    private readonly DateTime _now = new(2026, 10, 11, 10, 0, 0, DateTimeKind.Utc);

    public FinalDecisionPurchasedItemTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(_userId);
        _dateTime.Setup(s => s.UtcNow).Returns(_now);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(new ListMember
        {
            Id = Guid.NewGuid(),
            ListId = _listId,
            UserId = _userId,
            Role = MemberRole.Owner,
            JoinedAt = _now
        });
    }

    private ItemOption MakeOption(ItemStatus itemStatus, bool isFinal)
    {
        var item = new Item
        {
            Id = Guid.NewGuid(),
            ListId = _listId,
            CategoryId = Guid.NewGuid(),
            Name = "Test Item",
            Status = itemStatus,
            CreatedAt = _now
        };
        var option = new ItemOption
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            Title = "Option",
            IsFinal = isFinal,
            CreatedAt = _now,
            Item = item
        };
        _optionRepo.Setup(r => r.GetByIdWithItemAsync(option.Id, default)).ReturnsAsync(option);
        return option;
    }

    private SetFinalOptionCommandHandler CreateSetFinalHandler() => new(
        _optionRepo.Object, _listRepo.Object, _notifications.Object, _currentUser.Object, _dateTime.Object);

    private RemoveFinalDecisionCommandHandler CreateRemoveFinalHandler() => new(
        _optionRepo.Object, _listRepo.Object, _notifications.Object, _currentUser.Object);

    [Fact]
    public async Task SetFinal_OnPurchasedItem_ThrowsDomainException()
    {
        var option = MakeOption(ItemStatus.Purchased, isFinal: false);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateSetFinalHandler().Handle(new SetFinalOptionCommand(option.Id), default));

        Assert.False(option.IsFinal);
        _optionRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task SetFinal_OnPendingItem_FinalizesOption()
    {
        var option = MakeOption(ItemStatus.Pending, isFinal: false);
        _optionRepo.Setup(r => r.GetCurrentFinalOptionForItemAsync(option.ItemId, default))
            .ReturnsAsync((ItemOption?)null);

        await CreateSetFinalHandler().Handle(new SetFinalOptionCommand(option.Id), default);

        Assert.True(option.IsFinal);
        _optionRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RemoveFinal_OnPurchasedItem_ThrowsDomainException()
    {
        var option = MakeOption(ItemStatus.Purchased, isFinal: true);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateRemoveFinalHandler().Handle(new RemoveFinalDecisionCommand(option.Id), default));

        Assert.True(option.IsFinal);
        _optionRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
