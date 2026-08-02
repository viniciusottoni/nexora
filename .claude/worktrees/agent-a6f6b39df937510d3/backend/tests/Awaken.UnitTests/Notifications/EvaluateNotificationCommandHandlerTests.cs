using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.EvaluateNotification;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Notifications;

public class EvaluateNotificationCommandHandlerTests
{
    private readonly Mock<INotificationEligibilityService> _eligibilityService = new();
    private readonly Mock<INotificationLogRepository> _logRepo = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<EvaluateNotificationCommandHandler>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    public EvaluateNotificationCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
    }

    private EvaluateNotificationCommandHandler CreateHandler() => new(
        _eligibilityService.Object,
        _logRepo.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    // CA-001: allowed → logs "sent", returns allowed=true com logId válido
    [Fact]
    public async Task CA001_Allowed_LogsSentAndReturnsAllowed()
    {
        _eligibilityService
            .Setup(s => s.EvaluateAsync(UserId, "daily_quest_reminder", UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EligibilityResult.Allow());

        var result = await CreateHandler().Handle(
            new EvaluateNotificationCommand(UserId, "daily_quest_reminder"),
            CancellationToken.None);

        result.Allowed.Should().BeTrue();
        result.BlockReason.Should().BeNull();
        result.LogId.Should().NotBeEmpty();

        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l =>
                l.UserId == UserId &&
                l.NotificationType == "daily_quest_reminder" &&
                l.DecisionStatus == "sent" &&
                l.DecisionReason == null),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-001: blocked by limit → logs "ignored" com reason, retorna allowed=false
    [Fact]
    public async Task CA001_BlockedByLimit_LogsIgnoredWithReason()
    {
        _eligibilityService
            .Setup(s => s.EvaluateAsync(UserId, "daily_quest_reminder", UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EligibilityResult.Blocked("daily_limit_reached"));

        var result = await CreateHandler().Handle(
            new EvaluateNotificationCommand(UserId, "daily_quest_reminder"),
            CancellationToken.None);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("daily_limit_reached");

        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l =>
                l.DecisionStatus == "ignored" &&
                l.DecisionReason == "daily_limit_reached"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-002: qualquer decisão → persiste no log e salva
    [Fact]
    public async Task AnyDecision_PersistsLog()
    {
        _eligibilityService
            .Setup(s => s.EvaluateAsync(UserId, "streak_risk_alert", UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EligibilityResult.Blocked("no_consent"));

        await CreateHandler().Handle(
            new EvaluateNotificationCommand(UserId, "streak_risk_alert"),
            CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Verificação de campos do log: notificationType salvo corretamente
    [Fact]
    public async Task LogContainsCorrectNotificationType()
    {
        _eligibilityService
            .Setup(s => s.EvaluateAsync(UserId, "streak_risk_alert", UtcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EligibilityResult.Allow());

        await CreateHandler().Handle(
            new EvaluateNotificationCommand(UserId, "streak_risk_alert"),
            CancellationToken.None);

        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l => l.NotificationType == "streak_risk_alert"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
