using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.SendTrialEndingNotification;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Notifications;

public class SendTrialEndingNotificationCommandHandlerTests
{
    private readonly Mock<INotificationPreferenceRepository> _prefRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<IPushNotificationService> _pushService = new();
    private readonly Mock<INotificationLogRepository> _logRepo = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<SendTrialEndingNotificationCommandHandler>> _logger = new();

    private static readonly DateOnly Today = new(2026, 6, 27);
    private static readonly DateTime UtcNow = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    public SendTrialEndingNotificationCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _dateTimeService.Setup(d => d.TodayUtc).Returns(Today);
    }

    private SendTrialEndingNotificationCommandHandler CreateHandler() => new(
        _prefRepo.Object,
        _userRepo.Object,
        _subscriptionRepo.Object,
        _pushService.Object,
        _logRepo.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static NotificationPreference BuildPreference(Guid userId, bool pushEnabled = true, string? pushToken = "token-abc")
    {
        var pref = NotificationPreference.Create(userId, pushEnabled, pushToken, "granted", UtcNow);
        return pref;
    }

    private static User BuildUser(string language = "pt-BR")
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", language);
        return user;
    }

    private static Subscription BuildTrialEndingSoon(Guid userId)
        => Subscription.CreateTrial(userId, UtcNow.AddDays(-5), UtcNow.AddDays(2));

    private static Subscription BuildTrialNotEndingSoon(Guid userId)
        => Subscription.CreateTrial(userId, UtcNow.AddDays(-1), UtcNow.AddDays(7));

    private static Subscription BuildTrialExpired(Guid userId)
        => Subscription.CreateTrial(userId, UtcNow.AddDays(-8), UtcNow.AddDays(-1));

    private void SetupSinglePreference(NotificationPreference pref)
        => _prefRepo.Setup(r => r.GetAllWithPushEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([pref]);

    /// CA-001: trial proximo do fim, com consentimento → push enviado.
    [Fact]
    public async Task CA001_TrialEndingSoon_Sends()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser();
        var subscription = BuildTrialEndingSoon(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
        result.Skipped.Should().Be(0);
        result.Eligible.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync("token-abc", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// CA-002: assinante ativo nao recebe aviso.
    [Fact]
    public async Task CA002_ActiveSubscriber_DoesNotSend()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var subscription = Subscription.CreateFromPaidPlan(
            userId, "monthly", "premium", "rc-123", UtcNow.AddDays(30), UtcNow);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-002: trial nao proximo do fim (>3 dias) → nao envia.
    [Fact]
    public async Task RN002_TrialNotEndingSoon_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var subscription = BuildTrialNotEndingSoon(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-002: trial expirado → nao envia aviso de fim de trial (usa regra de reativacao).
    [Fact]
    public async Task RN002_TrialExpired_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var subscription = BuildTrialExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-004: limite diario atingido → nao envia.
    [Fact]
    public async Task RN004_DailyLimitReached_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        // 3 notificacoes enviadas hoje.
        for (var i = 0; i < 3; i++)
            pref.RecordNotificationSent(UtcNow);

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-001: push desabilitado → nao envia.
    [Fact]
    public async Task RN001_PushDisabled_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = NotificationPreference.Create(userId, false, null, "denied", UtcNow);

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// Push data payload deve incluir type=trial_ending_notification e route=/subscription.
    [Fact]
    public async Task PushDataPayload_ContainsCorrectTypeAndRoute()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser();
        var subscription = BuildTrialEndingSoon(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        Dictionary<string, string>? capturedData = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, _, _, data, _) => capturedData = data);

        await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        capturedData.Should().ContainKey("type").WhoseValue.Should().Be("trial_ending_notification");
        capturedData.Should().ContainKey("route").WhoseValue.Should().Be("/subscription");
    }

    /// Conteudo localizado pt-BR (default).
    [Fact]
    public async Task LocalizedContent_PTBR()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser("pt-BR");
        var subscription = BuildTrialEndingSoon(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        string? capturedTitle = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, _, _, _) => capturedTitle = title);

        await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        capturedTitle.Should().Contain("trial");
    }

    /// Conteudo localizado en.
    [Fact]
    public async Task LocalizedContent_EN()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser("en");
        var subscription = BuildTrialEndingSoon(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        string? capturedTitle = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, _, _, _) => capturedTitle = title);

        await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        capturedTitle.Should().Be("Your trial is ending soon!");
    }

    /// Conteudo localizado es.
    [Fact]
    public async Task LocalizedContent_ES()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser("es");
        var subscription = BuildTrialEndingSoon(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        string? capturedTitle = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, _, _, _) => capturedTitle = title);

        await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        capturedTitle.Should().Contain("prueba");
    }

    /// Falha no push registra decisao "failed" e nao quebra o job.
    [Fact]
    public async Task PushSendThrows_LogsFailedDecision()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser();
        var subscription = BuildTrialEndingSoon(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _pushService.Setup(p => p.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("push failed"));

        var result = await CreateHandler().Handle(new SendTrialEndingNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l =>
                l.UserId == userId &&
                l.NotificationType == "trial_ending_notification" &&
                l.DecisionStatus == "failed" &&
                l.DecisionReason == "push_send_failed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
