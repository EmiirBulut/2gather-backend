using Moq;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Members.Commands.InviteMember;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Tests.Features.Members;

public class InviteMemberCommandHandlerTests
{
    private readonly Mock<IListRepository> _listRepo = new();
    private readonly Mock<IListInviteRepository> _inviteRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeService> _dateTime = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _listId = Guid.NewGuid();
    private readonly DateTime _now = new(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc);

    private InviteMemberCommandHandler CreateHandler() => new(
        _listRepo.Object,
        _inviteRepo.Object,
        _emailService.Object,
        _currentUser.Object,
        _dateTime.Object);

    private Domain.Entities.List MakeList(IEnumerable<ListMember> members) => new()
    {
        Id = _listId,
        Name = "Test List",
        OwnerId = _userId,
        CreatedAt = _now,
        Members = members.ToList()
    };

    private ListMember MakeMember(MemberRole role) => new()
    {
        Id = Guid.NewGuid(),
        ListId = _listId,
        UserId = _userId,
        Role = role,
        JoinedAt = _now
    };

    public InviteMemberCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(_userId);
        _dateTime.Setup(s => s.UtcNow).Returns(_now);
    }

    [Theory]
    [InlineData(MemberRole.Owner)]
    [InlineData(MemberRole.Editor)]
    public async Task Handle_OwnerOrEditor_CreatesInviteAndSendsEmail(MemberRole callerRole)
    {
        _listRepo.Setup(r => r.GetByIdWithMembersAsync(_listId, default))
            .ReturnsAsync(MakeList(new[] { MakeMember(callerRole) }));

        ListInvite? capturedInvite = null;
        _inviteRepo.Setup(r => r.AddAsync(It.IsAny<ListInvite>(), default))
            .Callback<ListInvite, CancellationToken>((inv, _) => capturedInvite = inv)
            .Returns(Task.CompletedTask);

        var cmd = new InviteMemberCommand(_listId, "guest@example.com", MemberRole.Viewer);
        var result = await CreateHandler().Handle(cmd, default);

        Assert.NotNull(capturedInvite);
        Assert.Equal("guest@example.com", capturedInvite!.InvitedEmail);
        Assert.Equal(MemberRole.Viewer, capturedInvite.Role);
        Assert.Equal(_now.AddHours(48), capturedInvite.ExpiresAt);

        _inviteRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _emailService.Verify(e => e.SendInviteEmailAsync("guest@example.com", "Test List", capturedInvite.Token, default), Times.Once);

        Assert.Equal(capturedInvite.Id, result.InviteId);
        Assert.Equal(capturedInvite.Token, result.Token);
    }

    [Fact]
    public async Task Handle_ViewerCaller_ThrowsForbidden()
    {
        _listRepo.Setup(r => r.GetByIdWithMembersAsync(_listId, default))
            .ReturnsAsync(MakeList(new[] { MakeMember(MemberRole.Viewer) }));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new InviteMemberCommand(_listId, "guest@example.com", MemberRole.Viewer), default));

        _inviteRepo.Verify(r => r.AddAsync(It.IsAny<ListInvite>(), default), Times.Never);
        _emailService.Verify(e => e.SendInviteEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_NonMemberCaller_ThrowsForbidden()
    {
        _listRepo.Setup(r => r.GetByIdWithMembersAsync(_listId, default))
            .ReturnsAsync(MakeList(Array.Empty<ListMember>()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new InviteMemberCommand(_listId, "guest@example.com", MemberRole.Viewer), default));
    }

    [Fact]
    public async Task Handle_ListNotFound_ThrowsNotFoundException()
    {
        _listRepo.Setup(r => r.GetByIdWithMembersAsync(_listId, default))
            .ReturnsAsync((Domain.Entities.List?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new InviteMemberCommand(_listId, "guest@example.com", MemberRole.Viewer), default));
    }
}
