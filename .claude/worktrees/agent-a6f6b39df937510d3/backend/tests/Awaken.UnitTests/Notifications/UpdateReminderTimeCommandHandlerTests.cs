using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.UpdateReminderTime;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Notifications;

public class UpdateReminderTimeCommandHandlerTests
{
    private readonly Mock<INotificationPreferenceRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<UpdateReminderTimeCommandHandler>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 27, 23, 0, 0, DateTimeKind.Utc);
    private static readonly TimeOnly ReminderTime = new(19, 30);
    private const string Timezone = "America/Recife";

    public UpdateReminderTimeCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
    }

    private UpdateReminderTimeCommandHandler CreateHandler() => new(
        _repository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    [Fact]
    public async Task HandleCreatesNewPreferenceWithReminderTimeWhenNoneExists()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var command = new UpdateReminderTimeCommand(ReminderTime, Timezone);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.UserId.Should().Be(UserId);
        result.PreferredReminderTime.Should().Be(ReminderTime);
        result.Timezone.Should().Be(Timezone);
        result.UpdatedAtUtc.Should().Be(UtcNow);

        _repository.Verify(r => r.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleUpdatesExistingPreferenceReminderTime()
    {
        var existing = NotificationPreference.Create(UserId, true, "fcm-token", "granted", UtcNow.AddDays(-1));

        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new UpdateReminderTimeCommand(ReminderTime, Timezone);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PreferredReminderTime.Should().Be(ReminderTime);
        result.Timezone.Should().Be(Timezone);
        result.UpdatedAtUtc.Should().Be(UtcNow);

        _repository.Verify(r => r.Update(It.Is<NotificationPreference>(np =>
            np.PreferredReminderTime == ReminderTime &&
            np.Timezone == Timezone)),
            Times.Once);

        _repository.Verify(r => r.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleReturnsResponseWithPreferredReminderTimeAndTimezone()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var command = new UpdateReminderTimeCommand(new TimeOnly(8, 0), "America/Sao_Paulo");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PreferredReminderTime.Should().Be(new TimeOnly(8, 0));
        result.Timezone.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public async Task HandleUsesDateTimeServiceForUtcNow()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var command = new UpdateReminderTimeCommand(ReminderTime, Timezone);

        await CreateHandler().Handle(command, CancellationToken.None);

        _dateTimeService.Verify(d => d.UtcNow, Times.AtLeastOnce);
    }
}
