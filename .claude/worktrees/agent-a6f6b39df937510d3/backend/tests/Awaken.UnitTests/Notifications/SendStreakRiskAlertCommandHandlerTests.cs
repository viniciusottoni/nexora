using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.SendStreakRiskAlert;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Notifications;

public class SendStreakRiskAlertCommandHandlerTests
{
    private readonly Mock<INotificationPreferenceRepository> _prefRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<IQuestRepository> _questRepo = new();
    private readonly Mock<IHunterProgressionRepository> _progressionRepo = new();
    private readonly Mock<IPushNotificationService> _pushService = new();
    private readonly Mock<INotificationLogRepository> _logRepo = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<SendStreakRiskAlertCommandHandler>> _logger = new();

    private static readonly DateOnly Today = new(2026, 6, 27);
    private static readonly DateTime UtcNow = new(2026, 6, 27, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TodayUtc = new(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);

    public SendStreakRiskAlertCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _dateTimeService.Setup(d => d.TodayUtc).Returns(Today);
    }

    private SendStreakRiskAlertCommandHandler CreateHandler() => new(
        _prefRepo.Object,
        _userRepo.Object,
        _subscriptionRepo.Object,
        _questRepo.Object,
        _progressionRepo.Object,
        _pushService.Object,
        _logRepo.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static NotificationPreference BuildPreference(
        Guid userId,
        bool pushEnabled = true,
        string? pushToken = "token-streak",
        int dailyCount = 0,
        DateOnly? resetDate = null)
    {
        var pref = NotificationPreference.Create(userId, pushEnabled, pushToken, "granted", UtcNow);

        if (dailyCount > 0 && resetDate.HasValue)
        {
            var fakeUtcForToday = resetDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            for (var i = 0; i < dailyCount; i++)
                pref.RecordNotificationSent(fakeUtcForToday);
        }

        return pref;
    }

    private static User BuildActiveTrialUser(string language = "pt-BR")
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", language);
        user.StartTrial(UtcNow.AddDays(7));
        return user;
    }

    private static HunterProgression BuildProgressionWithStreak(Guid userId, int streakDays = 5)
    {
        var progression = HunterProgression.Create(userId);
        typeof(HunterProgression)
            .GetProperty(nameof(HunterProgression.CurrentStreakDays))!
            .SetValue(progression, streakDays);
        return progression;
    }

    private static Quest BuildPendingQuest(Guid userId) =>
        Quest.Create(userId, TodayUtc, "pt-BR", $"{userId:N}_daily");

    private void SetupSinglePreference(NotificationPreference pref)
        => _prefRepo.Setup(r => r.GetAllWithPushEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([pref]);

    [Fact]
    public async Task CA001_ActiveStreak_QuestNotCompleted_Sends()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithStreak(userId, streakDays: 5);
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
        result.Skipped.Should().Be(0);
        result.Eligible.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync("token-streak", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _prefRepo.Verify(r => r.Update(pref), Times.Once);
    }

    [Fact]
    public async Task CA002_QuestCompleted_DoesNotSend()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithStreak(userId, streakDays: 3);
        var quest = BuildPendingQuest(userId);
        quest.Complete(100, DateTime.UtcNow);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RN003_NoActiveStreak_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithStreak(userId, streakDays: 0);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RN003_NullProgression_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RN001_PushDisabled_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = NotificationPreference.Create(userId, false, null, "denied", UtcNow);

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RN002_InactiveAccess_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(-1));

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RN005_DailyLimitReached_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId, dailyCount: 3, resetDate: Today);

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LocalizedContent_EN()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser("en");
        var progression = BuildProgressionWithStreak(userId, streakDays: 7);
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        string? capturedTitle = null;
        string? capturedBody = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, body, _, _) => { capturedTitle = title; capturedBody = body; });

        await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        capturedTitle.Should().Be("Your streak is at risk!");
        capturedBody.Should().Be("Complete your quest today to keep your streak alive.");
    }

    /// RN-007: falha no push deve registrar decisão failed e não quebrar o job.
    [Fact]
    public async Task PushSendThrows_LogsFailedDecision()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithStreak(userId, streakDays: 7);
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _pushService.Setup(p => p.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("push failed"));

        var result = await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l =>
                l.UserId == userId &&
                l.NotificationType == "streak_risk_alert" &&
                l.DecisionStatus == "failed" &&
                l.DecisionReason == "push_send_failed"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LocalizedContent_ES()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser("es");
        var progression = BuildProgressionWithStreak(userId, streakDays: 10);
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        string? capturedTitle = null;
        string? capturedBody = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, body, _, _) => { capturedTitle = title; capturedBody = body; });

        await CreateHandler().Handle(new SendStreakRiskAlertCommand(), CancellationToken.None);

        capturedTitle.Should().Be("¡Tu racha está en riesgo!");
        capturedBody.Should().Be("Completa tu quest hoy para mantener la racha.");
    }
}
