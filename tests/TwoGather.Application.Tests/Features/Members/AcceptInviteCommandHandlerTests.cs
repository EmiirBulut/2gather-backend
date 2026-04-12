using Moq;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Members.Commands.AcceptInvite;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Tests.Features.Members;

public class AcceptInviteCommandHandlerTests
{
    private readonly Mock<IListInviteRepository> _inviteRepo = new();
    private readonly Mock<IListRepository> _listRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeService> _dateTime = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _listId = Guid.NewGuid();
    private readonly DateTime _now = new(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc);
    private const string ValidToken = "abc123token";

    private AcceptInviteCommandHandler CreateHandler() => new(
        _inviteRepo.Object,
        _listRepo.Object,
        _userRepo.Object,
        _currentUser.Object,
        _dateTime.Object);

    private ListInvite MakeInvite(DateTime? acceptedAt = null, DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        ListId = _listId,
        InvitedEmail = "guest@example.com",
        Token = ValidToken,
        Role = MemberRole.Editor,
        ExpiresAt = expiresAt ?? _now.AddHours(48),
        AcceptedAt = acceptedAt,
        CreatedAt = _now.AddHours(-1)
    };

    private User MakeUser() => new()
    {
        Id = _userId,
        Email = "guest@example.com",
        DisplayName = "Guest",
        PasswordHash = "hash",
        CreatedAt = _now
    };

    public AcceptInviteCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(_userId);
        _dateTime.Setup(s => s.UtcNow).Returns(_now);
    }

    [Fact]
    public async Task Handle_ValidToken_CreatesMemberAndMarksAccepted()
    {
        var invite = MakeInvite();
        _inviteRepo.Setup(r => r.GetByTokenAsync(ValidToken, default)).ReturnsAsync(invite);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync((ListMember?)null);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, default)).ReturnsAsync(MakeUser());

        ListMember? capturedMember = null;
        _listRepo.Setup(r => r.AddMemberAsync(It.IsAny<ListMember>(), default))
            .Callback<ListMember, CancellationToken>((m, _) => capturedMember = m)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(new AcceptInviteCommand(ValidToken), default);

        Assert.NotNull(capturedMember);
        Assert.Equal(_userId, capturedMember!.UserId);
        Assert.Equal(_listId, capturedMember.ListId);
        Assert.Equal(MemberRole.Editor, capturedMember.Role);

        _inviteRepo.Verify(r => r.UpdateAcceptedAtAsync(It.Is<ListInvite>(i => i.AcceptedAt == _now), default), Times.Once);
        _inviteRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);

        Assert.Equal(_userId, result.UserId);
        Assert.Equal(MemberRole.Editor, result.Role);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsDomainException()
    {
        var invite = MakeInvite(expiresAt: _now.AddHours(-1));
        _inviteRepo.Setup(r => r.GetByTokenAsync(ValidToken, default)).ReturnsAsync(invite);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(new AcceptInviteCommand(ValidToken), default));

        _listRepo.Verify(r => r.AddMemberAsync(It.IsAny<ListMember>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyAcceptedToken_ThrowsDomainException()
    {
        var invite = MakeInvite(acceptedAt: _now.AddHours(-2));
        _inviteRepo.Setup(r => r.GetByTokenAsync(ValidToken, default)).ReturnsAsync(invite);

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(new AcceptInviteCommand(ValidToken), default));
    }

    [Fact]
    public async Task Handle_AlreadyMember_ThrowsDomainException()
    {
        var invite = MakeInvite();
        _inviteRepo.Setup(r => r.GetByTokenAsync(ValidToken, default)).ReturnsAsync(invite);
        _listRepo.Setup(r => r.GetMemberAsync(_listId, _userId, default)).ReturnsAsync(new ListMember
        {
            Id = Guid.NewGuid(), ListId = _listId, UserId = _userId, Role = MemberRole.Viewer, JoinedAt = _now
        });

        await Assert.ThrowsAsync<DomainException>(() =>
            CreateHandler().Handle(new AcceptInviteCommand(ValidToken), default));
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsNotFoundException()
    {
        _inviteRepo.Setup(r => r.GetByTokenAsync(ValidToken, default)).ReturnsAsync((ListInvite?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new AcceptInviteCommand(ValidToken), default));
    }
}
