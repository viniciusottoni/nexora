using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.UpdatePreferences;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Notifications;

public class UpdateNotificationPreferencesCommandHandlerTests
{
    private readonly Mock<INotificationPreferenceRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<UpdateNotificationPreferencesCommandHandler>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    public UpdateNotificationPreferencesCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
    }

    private UpdateNotificationPreferencesCommandHandler CreateHandler() => new(
        _repository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    [Fact]
    public async Task HandleCreatesNewPreferenceWhenNoneExists()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var command = new UpdateNotificationPreferencesCommand(true, "fcm-token-abc", "granted");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.UserId.Should().Be(UserId);
        result.PushEnabled.Should().BeTrue();
        result.PushToken.Should().Be("fcm-token-abc");
        result.PermissionStatus.Should().Be("granted");
        result.UpdatedAtUtc.Should().Be(UtcNow);

        _repository.Verify(r => r.AddAsync(It.Is<NotificationPreference>(np =>
            np.UserId == UserId &&
            np.PushEnabled == true &&
            np.PushToken == "fcm-token-abc" &&
            np.PermissionStatus == "granted"),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleUpdatesExistingPreference()
    {
        var existing = NotificationPreference.Create(UserId, false, null, "not_requested", UtcNow.AddDays(-1));

        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new UpdateNotificationPreferencesCommand(true, "new-fcm-token", "token_registered");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PushEnabled.Should().BeTrue();
        result.PushToken.Should().Be("new-fcm-token");
        result.PermissionStatus.Should().Be("token_registered");
        result.UpdatedAtUtc.Should().Be(UtcNow);

        _repository.Verify(r => r.Update(It.Is<NotificationPreference>(np =>
            np.PushEnabled == true &&
            np.PushToken == "new-fcm-token" &&
            np.PermissionStatus == "token_registered")),
            Times.Once);

        _repository.Verify(r => r.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleClearsPushTokenWhenPushDisabled()
    {
        var existing = NotificationPreference.Create(UserId, true, "old-fcm-token", "token_registered", UtcNow.AddDays(-1));

        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new UpdateNotificationPreferencesCommand(false, "old-fcm-token", "denied");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PushEnabled.Should().BeFalse();
        result.PushToken.Should().BeNull();
        result.PermissionStatus.Should().Be("denied");

        _repository.Verify(r => r.Update(It.Is<NotificationPreference>(np =>
            np.PushEnabled == false &&
            np.PushToken == null)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAllowsNullTokenWithPushEnabled()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var command = new UpdateNotificationPreferencesCommand(true, null, "granted");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PushEnabled.Should().BeTrue();
        result.PushToken.Should().BeNull();
        result.PermissionStatus.Should().Be("granted");

        _repository.Verify(r => r.AddAsync(It.Is<NotificationPreference>(np =>
            np.PushEnabled == true &&
            np.PushToken == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleUsesDefaultPermissionStatusWhenNull()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var command = new UpdateNotificationPreferencesCommand(false, null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PermissionStatus.Should().Be("not_requested");
    }
}
