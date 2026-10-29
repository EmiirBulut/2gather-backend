using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TwoGather.Application.Common.Interfaces;
using TwoGather.Application.Features.Lists.Commands.RenameList;
using TwoGather.Application.Features.Users.Commands.ChangePassword;
using TwoGather.Application.Features.Users.Commands.UpdateProfile;
using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Tests.Features.Users;

public class UserSettingsHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IListRepository> _listRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly User _user;

    public UserSettingsHandlerTests()
    {
        _user = new User { Id = _userId, Email = "ayse@example.com", DisplayName = "Ayşe", PasswordHash = "hash" };
        _currentUser.Setup(s => s.UserId).Returns(_userId);
        _userRepo.Setup(r => r.GetByIdAsync(_userId, default)).ReturnsAsync(_user);
        _hasher.Setup(h => h.Verify("correct-pass", "hash")).Returns(true);
    }

    private UpdateProfileCommandHandler ProfileHandler() => new(
        _userRepo.Object, _hasher.Object, _currentUser.Object, NullLogger<UpdateProfileCommandHandler>.Instance);

    [Fact]
    public async Task UpdateProfile_NameOnly_DoesNotNeedPassword()
    {
        var result = await ProfileHandler().Handle(new UpdateProfileCommand(" Ayşe B. ", "AYSE@example.com", null), default);

        Assert.Equal("Ayşe B.", result.DisplayName);
        Assert.Equal("ayse@example.com", result.Email);
        _userRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-pass")]
    public async Task UpdateProfile_EmailChangeWithoutValidPassword_Throws(string? password)
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            ProfileHandler().Handle(new UpdateProfileCommand("Ayşe", "new@example.com", password), default));

        Assert.Equal("ayse@example.com", _user.Email);
        _userRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_EmailTaken_Throws()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("taken@example.com", default)).ReturnsAsync(new User { Id = Guid.NewGuid() });

        await Assert.ThrowsAsync<DomainException>(() =>
            ProfileHandler().Handle(new UpdateProfileCommand("Ayşe", "taken@example.com", "correct-pass"), default));
    }

    [Fact]
    public async Task UpdateProfile_EmailChangeWithPassword_UpdatesEmail()
    {
        var result = await ProfileHandler().Handle(new UpdateProfileCommand("Ayşe", "New@Example.com", "correct-pass"), default);

        Assert.Equal("new@example.com", result.Email);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Throws()
    {
        var handler = new ChangePasswordCommandHandler(
            _userRepo.Object, _hasher.Object, _currentUser.Object, NullLogger<ChangePasswordCommandHandler>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ChangePasswordCommand("wrong-pass", "new-password-123"), default));
        _userRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrent_StoresNewHash()
    {
        _hasher.Setup(h => h.Hash("new-password-123")).Returns("new-hash");
        var handler = new ChangePasswordCommandHandler(
            _userRepo.Object, _hasher.Object, _currentUser.Object, NullLogger<ChangePasswordCommandHandler>.Instance);

        await handler.Handle(new ChangePasswordCommand("correct-pass", "new-password-123"), default);

        Assert.Equal("new-hash", _user.PasswordHash);
    }

    [Theory]
    [InlineData(MemberRole.Editor)]
    [InlineData(MemberRole.Viewer)]
    public async Task RenameList_NonOwner_ThrowsForbidden(MemberRole role)
    {
        var list = new Domain.Entities.List { Id = Guid.NewGuid(), Name = "Ev" };
        _listRepo.Setup(r => r.GetByIdAsync(list.Id, default)).ReturnsAsync(list);
        _listRepo.Setup(r => r.GetMemberAsync(list.Id, _userId, default))
            .ReturnsAsync(new ListMember { ListId = list.Id, UserId = _userId, Role = role });
        var handler = new RenameListCommandHandler(_listRepo.Object, _notifications.Object, _currentUser.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new RenameListCommand(list.Id, "Yeni Ev"), default));
        Assert.Equal("Ev", list.Name);
    }

    [Fact]
    public async Task RenameList_Owner_RenamesAndNotifies()
    {
        var list = new Domain.Entities.List { Id = Guid.NewGuid(), Name = "Ev" };
        _listRepo.Setup(r => r.GetByIdAsync(list.Id, default)).ReturnsAsync(list);
        _listRepo.Setup(r => r.GetMemberAsync(list.Id, _userId, default))
            .ReturnsAsync(new ListMember { ListId = list.Id, UserId = _userId, Role = MemberRole.Owner });
        var handler = new RenameListCommandHandler(_listRepo.Object, _notifications.Object, _currentUser.Object);

        await handler.Handle(new RenameListCommand(list.Id, "  Yeni Ev "), default);

        Assert.Equal("Yeni Ev", list.Name);
        _notifications.Verify(n => n.ListUpdatedAsync(list.Id, "Yeni Ev", default), Times.Once);
    }
}
