using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Members.Commands.ProcessInviteEmailEvent;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;

namespace TwoGather.Application.Tests.Features.Members;

public class ProcessInviteEmailEventTests
{
    private readonly Mock<IListInviteRepository> _inviteRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly DateTime _now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);

    private ProcessInviteEmailEventCommandHandler CreateHandler() => new(
        _inviteRepo.Object, _notifications.Object, NullLogger<ProcessInviteEmailEventCommandHandler>.Instance);

    [Theory]
    [InlineData(InviteEmailStatus.Sent, "email.delivered", InviteEmailStatus.Delivered)]
    [InlineData(InviteEmailStatus.Pending, "email.sent", InviteEmailStatus.Sent)]
    [InlineData(InviteEmailStatus.Sent, "email.bounced", InviteEmailStatus.Bounced)]
    [InlineData(InviteEmailStatus.Delivered, "email.complained", InviteEmailStatus.Complained)]
    [InlineData(InviteEmailStatus.Sent, "email.failed", InviteEmailStatus.Failed)]
    public void NextStatus_ForwardTransitions(InviteEmailStatus current, string eventType, InviteEmailStatus expected)
        => Assert.Equal(expected, ProcessInviteEmailEventCommandHandler.NextStatus(current, eventType));

    [Theory]
    [InlineData(InviteEmailStatus.Delivered, "email.sent")]
    [InlineData(InviteEmailStatus.Delivered, "email.failed")]
    [InlineData(InviteEmailStatus.Complained, "email.bounced")]
    [InlineData(InviteEmailStatus.Sent, "email.opened")]
    [InlineData(InviteEmailStatus.Sent, "email.delivery_delayed")]
    public void NextStatus_OutOfOrderOrIrrelevantEvents_AreIgnored(InviteEmailStatus current, string eventType)
        => Assert.Null(ProcessInviteEmailEventCommandHandler.NextStatus(current, eventType));

    [Fact]
    public async Task Handle_Delivered_UpdatesInviteAndNotifies()
    {
        var invite = new ListInvite { Id = Guid.NewGuid(), ListId = Guid.NewGuid(), EmailStatus = InviteEmailStatus.Sent, ProviderMessageId = "m1" };
        _inviteRepo.Setup(r => r.GetByProviderMessageIdAsync("m1", default)).ReturnsAsync(invite);

        await CreateHandler().Handle(new ProcessInviteEmailEventCommand("email.delivered", "m1", _now), default);

        Assert.Equal(InviteEmailStatus.Delivered, invite.EmailStatus);
        Assert.Equal(_now, invite.DeliveredAt);
        _inviteRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _notifications.Verify(n => n.InviteStatusChangedAsync(invite.ListId, invite.Id, default), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownMessage_DoesNothing()
    {
        _inviteRepo.Setup(r => r.GetByProviderMessageIdAsync("old", default)).ReturnsAsync((ListInvite?)null);

        await CreateHandler().Handle(new ProcessInviteEmailEventCommand("email.delivered", "old", _now), default);

        _inviteRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
