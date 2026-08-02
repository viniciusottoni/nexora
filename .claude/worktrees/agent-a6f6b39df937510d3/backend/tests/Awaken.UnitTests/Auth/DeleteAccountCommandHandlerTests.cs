using Awaken.Application.Auth.Commands.DeleteAccount;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Auth;

public class DeleteAccountCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private DeleteAccountCommandHandler CreateHandler() => new(
        _currentUserService.Object,
        _userRepository.Object,
        _refreshTokenRepository.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    public DeleteAccountCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task HandleSoftDeletesUserRevokesTokensAndSaves()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-pw", "Hunter");
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        result.Should().Be(Unit.Value);
        user.IsDeleted.Should().BeTrue();
        user.DeletedAtUtc.Should().NotBeNull();
        _refreshTokenRepository.Verify(
            r => r.RevokeAllByUserIdAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleSoftDeletesUserBeforeRevokingTokens()
    {
        var callOrder = new List<string>();
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-pw", "Hunter");
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository
            .Setup(r => r.Update(It.IsAny<User>()))
            .Callback(() => callOrder.Add("update"));
        _refreshTokenRepository
            .Setup(r => r.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("revoke"))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        callOrder.Should().ContainInOrder("update", "revoke", "save");
    }

    [Fact]
    public async Task HandleSetsDeletedAtUtcOnUser()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-pw", "Hunter");
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        user.DeletedAtUtc.Should().Be(new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task HandleThrowsWhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleRecordsAccountDeletedAuditLog()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-pw", "Hunter");
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        _auditLogService.Verify(
            a => a.RecordAsync(
                "account_deleted",
                userId,
                AuditActorType.User,
                "User",
                userId,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleRecordsAuditBeforeSave()
    {
        var callOrder = new List<string>();
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-pw", "Hunter");
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _auditLogService
            .Setup(a => a.RecordAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("audit"))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        callOrder.Should().ContainInOrder("audit", "save");
    }
}
