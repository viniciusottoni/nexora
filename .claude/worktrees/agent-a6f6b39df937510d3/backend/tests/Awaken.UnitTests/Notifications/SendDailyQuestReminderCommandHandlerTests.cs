using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.SendDailyQuestReminder;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Notifications;

public class SendDailyQuestReminderCommandHandlerTests
{
    private readonly Mock<INotificationPreferenceRepository> _prefRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<IQuestRepository> _questRepo = new();
    private readonly Mock<IPushNotificationService> _pushService = new();
    private readonly Mock<INotificationLogRepository> _logRepo = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<SendDailyQuestReminderCommandHandler>> _logger = new();

    // Current time: 09:00 UTC — before the default 20:00 preferred time test scenario.
    private static readonly DateOnly Today = new(2026, 6, 27);
    private static readonly DateTime UtcNow = new(2026, 6, 27, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TodayUtc = new(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc);

    public SendDailyQuestReminderCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _dateTimeService.Setup(d => d.TodayUtc).Returns(Today);
    }

    private void SetUtcNow(DateTime utcNow)
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(utcNow);
        _dateTimeService.Setup(d => d.TodayUtc).Returns(DateOnly.FromDateTime(utcNow));
    }

    private SendDailyQuestReminderCommandHandler CreateHandler() => new(
        _prefRepo.Object,
        _userRepo.Object,
        _subscriptionRepo.Object,
        _questRepo.Object,
        _pushService.Object,
        _logRepo.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static NotificationPreference BuildPreference(
        Guid userId,
        bool pushEnabled = true,
        string? pushToken = "token-abc",
        int dailyCount = 0,
        DateOnly? resetDate = null,
        TimeOnly? preferredTime = null)
    {
        var pref = NotificationPreference.Create(userId, pushEnabled, pushToken, "granted", UtcNow);

        // Use RecordNotificationSent to drive the counter state when needed.
        // For custom state, rely on RecordNotificationSent in test setup.
        if (dailyCount > 0 && resetDate.HasValue)
        {
            // Simulate having sent <dailyCount> notifications today by calling RecordNotificationSent.
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

    private static Quest BuildPendingQuest(Guid userId) =>
        Quest.Create(userId, TodayUtc, "pt-BR", $"{userId:N}_daily");

    private void SetupSinglePreference(NotificationPreference pref)
        => _prefRepo.Setup(r => r.GetPageWithPushEnabledAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([pref]);

    /// CA001: usuario elegivel com quest pendente deve receber push e ter o envio registrado.
    [Fact]
    public async Task CA001_EligibleUser_QuestNotCompleted_Sends()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
        result.Skipped.Should().Be(0);
        result.Eligible.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync("token-abc", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// CA002: quest completada hoje — nao envia push.
    [Fact]
    public async Task CA002_QuestCompleted_DoesNotSend()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var quest = BuildPendingQuest(userId);
        quest.Complete(100, DateTime.UtcNow);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN001: push desabilitado ou token nulo — nao envia push (filtrado antes pelo repositorio,
    /// mas CanReceiveNotificationToday tambem protege).
    [Fact]
    public async Task RN001_PushDisabled_Skips()
    {
        var userId = Guid.NewGuid();
        // Repositorio retorna preferencia com PushEnabled=false (simula falha de filtro ou teste unitario).
        var pref = NotificationPreference.Create(userId, false, null, "denied", UtcNow);

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN002: trial expirado — nao envia push.
    [Fact]
    public async Task RN002_InactiveAccess_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(-1)); // trial expirado

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN004: limite diario de 3 notificacoes atingido — nao envia.
    [Fact]
    public async Task RN004_DailyLimitReached_Skips()
    {
        var userId = Guid.NewGuid();
        // 3 notificacoes ja enviadas hoje (limite maximo).
        var pref = BuildPreference(userId, dailyCount: 3, resetDate: Today);

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN005: horario preferido nao atingido (preferencia 20:00, hora atual 09:00) — nao envia.
    [Fact]
    public async Task RN005_PreferredTimeNotYetReached_Skips()
    {
        var userId = Guid.NewGuid();
        // UtcNow = 09:00; preferred = 20:00 → ainda nao chegou no horario.
        var pref = BuildPreference(userId);

        // Simula preferencia de horario via reflexao (propriedade privada no dominio).
        typeof(NotificationPreference)
            .GetProperty(nameof(NotificationPreference.PreferredReminderTime))!
            .SetValue(pref, new TimeOnly(20, 0));

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN005: 11:30 UTC em America/Sao_Paulo equivale a 08:30 local, então 09:30 ainda não foi atingido.
    [Fact]
    public async Task RN005_PreferredTimeInUserTimezoneNotYetReached_Skips()
    {
        var utcNow = new DateTime(2026, 6, 27, 11, 30, 0, DateTimeKind.Utc);
        SetUtcNow(utcNow);

        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        pref.UpdateReminderTime(new TimeOnly(9, 30), "America/Sao_Paulo", utcNow);
        var user = BuildActiveTrialUser();
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN005: 12:30 UTC em America/Sao_Paulo equivale a 09:30 local, então o lembrete deve ser enviado.
    [Fact]
    public async Task RN005_PreferredTimeInUserTimezoneReached_Sends()
    {
        var utcNow = new DateTime(2026, 6, 27, 12, 30, 0, DateTimeKind.Utc);
        SetUtcNow(utcNow);

        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        pref.UpdateReminderTime(new TimeOnly(9, 30), "America/Sao_Paulo", utcNow);
        var user = BuildActiveTrialUser();
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
        result.Skipped.Should().Be(0);
        _pushService.Verify(
            p => p.SendAsync("token-abc", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// RN005: timezone inválido cai para UTC e continua aplicando a comparação de horário.
    [Fact]
    public async Task RN005_InvalidTimezoneFallsBackToUtc()
    {
        var utcNow = new DateTime(2026, 6, 27, 11, 30, 0, DateTimeKind.Utc);
        SetUtcNow(utcNow);

        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        pref.UpdateReminderTime(new TimeOnly(12, 0), "Invalid/Zone", utcNow);
        var user = BuildActiveTrialUser();
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// Conteudo localizado para pt-BR (default).
    [Fact]
    public async Task LocalizedContent_PT_BR()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser("pt-BR");

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPendingQuest(userId));

        string? capturedTitle = null;
        string? capturedBody = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, body, _, _) => { capturedTitle = title; capturedBody = body; });

        await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        capturedTitle.Should().Contain("Hunter");
        capturedBody.Should().Contain("quest");
    }

    /// Conteudo localizado para en.
    [Fact]
    public async Task LocalizedContent_EN()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser("en");

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPendingQuest(userId));

        string? capturedTitle = null;
        string? capturedBody = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, body, _, _) => { capturedTitle = title; capturedBody = body; });

        await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        capturedTitle.Should().Be("Your quest awaits, Hunter!");
        capturedBody.Should().Be("Don't forget to complete your daily quest.");
    }

    /// Push data payload deve incluir type e route corretos.
    [Fact]
    public async Task PushDataPayload_ContainsCorrectTypeAndRoute()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPendingQuest(userId));

        Dictionary<string, string>? capturedData = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, _, _, data, _) => capturedData = data);

        await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        capturedData.Should().ContainKey("type").WhoseValue.Should().Be("daily_quest_reminder");
        capturedData.Should().ContainKey("route").WhoseValue.Should().Be("/daily-quest");
    }

    /// RN-007: falha no push deve registrar decisão failed e não quebrar o job.
    [Fact]
    public async Task PushSendThrows_LogsFailedDecision()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildActiveTrialUser();
        var quest = BuildPendingQuest(userId);

        SetupSinglePreference(pref);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _questRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, "daily", TodayUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _pushService.Setup(p => p.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("push failed"));

        var result = await CreateHandler().Handle(new SendDailyQuestReminderCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l =>
                l.UserId == userId &&
                l.NotificationType == "daily_quest_reminder" &&
                l.DecisionStatus == "failed" &&
                l.DecisionReason == "push_send_failed"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
