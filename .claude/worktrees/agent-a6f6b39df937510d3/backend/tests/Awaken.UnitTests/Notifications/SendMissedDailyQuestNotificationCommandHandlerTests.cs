using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.SendMissedDailyQuestNotification;
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

public class SendMissedDailyQuestNotificationCommandHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepo = new();
    private readonly Mock<INotificationPreferenceRepository> _prefRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<IHunterProgressionRepository> _progressionRepo = new();
    private readonly Mock<IPushNotificationService> _pushService = new();
    private readonly Mock<INotificationLogRepository> _logRepo = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<SendMissedDailyQuestNotificationCommandHandler>> _logger = new();

    private static readonly DateOnly Today = new(2026, 6, 28);
    private static readonly DateTime UtcNow = new(2026, 6, 28, 1, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime YesterdayUtc = new(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);

    public SendMissedDailyQuestNotificationCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _dateTimeService.Setup(d => d.TodayUtc).Returns(Today);
    }

    private SendMissedDailyQuestNotificationCommandHandler CreateHandler() => new(
        _questRepo.Object,
        _prefRepo.Object,
        _userRepo.Object,
        _subscriptionRepo.Object,
        _progressionRepo.Object,
        _pushService.Object,
        _logRepo.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static Quest BuildMissedQuest(Guid userId)
    {
        var quest = Quest.Create(userId, YesterdayUtc, "pt-BR", $"{userId:N}_daily");
        quest.MarkPenaltyChecked(YesterdayUtc.AddHours(1));
        return quest;
    }

    private static NotificationPreference BuildPreference(
        Guid userId,
        bool pushEnabled = true,
        string? pushToken = "token-missed",
        int dailyCount = 0,
        DateOnly? resetDate = null)
    {
        var pref = NotificationPreference.Create(userId, pushEnabled, pushToken, "granted", UtcNow);
        if (dailyCount > 0 && resetDate.HasValue)
        {
            var fakeUtc = resetDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            for (var i = 0; i < dailyCount; i++)
                pref.RecordNotificationSent(fakeUtc);
        }
        return pref;
    }

    private static User BuildActiveTrialUser(string language = "pt-BR")
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", language);
        user.StartTrial(UtcNow.AddDays(7));
        return user;
    }

    private static HunterProgression BuildProgressionWithPenalty(Guid userId, long penaltyXp = 10)
    {
        var progression = HunterProgression.Create(userId);
        typeof(HunterProgression)
            .GetProperty(nameof(HunterProgression.RecentDailyPenaltyXp))!
            .SetValue(progression, penaltyXp);
        return progression;
    }

    private void SetupSingleMissedQuest(Quest quest)
        => _questRepo.Setup(r => r.GetMissedPenaltyCheckedByDateAsync(
                YesterdayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync([quest]);

    private void SetupNoMissedQuests()
        => _questRepo.Setup(r => r.GetMissedPenaltyCheckedByDateAsync(
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    /// CA-001: quest perdida com penalidade e acesso ativo → envia.
    [Fact]
    public async Task CA001_MissedQuestWithPenalty_ActiveAccess_Sends()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithPenalty(userId, penaltyXp: 10);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
        result.Skipped.Should().Be(0);
        result.Eligible.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync("token-missed", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _prefRepo.Verify(r => r.Update(pref), Times.Once);
    }

    /// CA-002: nenhuma quest perdida → nenhum envio.
    [Fact]
    public async Task CA002_NoMissedQuests_SendsNothing()
    {
        SetupNoMissedQuests();

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Eligible.Should().Be(0);
        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(0);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-002: acesso expirado → não envia.
    [Fact]
    public async Task RN002_InactiveAccess_Skips()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(-1));

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-004: progressão nula → não envia (sem penalidade confirmada).
    [Fact]
    public async Task RN004_NullProgression_Skips()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HunterProgression?)null);

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-004: RecentDailyPenaltyXp = 0 → penalidade não foi aplicada, não envia.
    [Fact]
    public async Task RN004_ZeroPenalty_Skips()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithPenalty(userId, penaltyXp: 0);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-001/RN-006: push desabilitado → não envia.
    [Fact]
    public async Task RN001_PushDisabled_Skips()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = NotificationPreference.Create(userId, false, null, "denied", UtcNow);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-001: preferência nula → não envia.
    [Fact]
    public async Task RN001_NullPreference_Skips()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
    }

    /// RN-006: limite diário atingido → não envia.
    [Fact]
    public async Task RN006_DailyLimitReached_Skips()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId, dailyCount: 3, resetDate: Today);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// Push data deve conter type=missed_daily_quest_notification e route=/daily-quest.
    [Fact]
    public async Task PushDataPayload_ContainsCorrectTypeAndRoute()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithPenalty(userId);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        Dictionary<string, string>? capturedData = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, _, _, data, _) => capturedData = data);

        await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        capturedData.Should().ContainKey("type").WhoseValue.Should().Be("missed_daily_quest_notification");
        capturedData.Should().ContainKey("route").WhoseValue.Should().Be("/daily-quest");
    }

    /// Conteúdo localizado EN — tom encorajador.
    [Fact]
    public async Task LocalizedContent_EN()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser("en");
        var progression = BuildProgressionWithPenalty(userId);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        string? capturedTitle = null;
        string? capturedBody = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, body, _, _) => { capturedTitle = title; capturedBody = body; });

        await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        capturedTitle.Should().Be("You missed yesterday's quest");
        capturedBody.Should().Contain("Hunter");
    }

    /// Conteúdo localizado ES — tom encorajador.
    [Fact]
    public async Task LocalizedContent_ES()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser("es");
        var progression = BuildProgressionWithPenalty(userId);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        string? capturedTitle = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, _, _, _) => capturedTitle = title);

        await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        capturedTitle.Should().Be("Perdiste la quest de ayer");
    }

    /// Conteúdo localizado PT-BR (default) — tom encorajador.
    [Fact]
    public async Task LocalizedContent_PtBr_Default()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser("pt-BR");
        var progression = BuildProgressionWithPenalty(userId);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        string? capturedTitle = null;
        string? capturedBody = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, body, _, _) => { capturedTitle = title; capturedBody = body; });

        await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        capturedTitle.Should().Be("Você perdeu a quest de ontem");
        capturedBody.Should().Contain("Hunter");
    }

    /// Falha no push → registra decisão "failed" e não quebra o job.
    [Fact]
    public async Task PushSendThrows_LogsFailedDecision()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithPenalty(userId);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);
        _pushService.Setup(p => p.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("push failed"));

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l =>
                l.UserId == userId &&
                l.NotificationType == "missed_daily_quest_notification" &&
                l.DecisionStatus == "failed" &&
                l.DecisionReason == "push_send_failed"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// Assinante ativo com quest perdida e penalidade → envia.
    [Fact]
    public async Task ActiveSubscriber_MissedQuestWithPenalty_Sends()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var subscription = Subscription.CreateFromPaidPlan(
            userId, "monthly", "premium", "rc-123", UtcNow.AddDays(30), UtcNow);
        var progression = BuildProgressionWithPenalty(userId, penaltyXp: 20);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        var result = await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync("token-missed", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// Após envio, registra DecisionStatus=sent no log.
    [Fact]
    public async Task AfterSend_LogsSentDecision()
    {
        var userId = Guid.NewGuid();
        var quest = BuildMissedQuest(userId);
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var progression = BuildProgressionWithPenalty(userId);

        SetupSingleMissedQuest(quest);
        _prefRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _progressionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progression);

        await CreateHandler().Handle(new SendMissedDailyQuestNotificationCommand(), CancellationToken.None);

        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l =>
                l.UserId == userId &&
                l.NotificationType == "missed_daily_quest_notification" &&
                l.DecisionStatus == "sent"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
