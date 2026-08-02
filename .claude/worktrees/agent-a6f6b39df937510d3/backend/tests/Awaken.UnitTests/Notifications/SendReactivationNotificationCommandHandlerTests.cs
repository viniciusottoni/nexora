using Awaken.Application.Common.Interfaces;
using Awaken.Application.Notifications.Commands.SendReactivationNotification;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Notifications;

public class SendReactivationNotificationCommandHandlerTests
{
    private readonly Mock<INotificationPreferenceRepository> _prefRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<IPushNotificationService> _pushService = new();
    private readonly Mock<INotificationLogRepository> _logRepo = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<SendReactivationNotificationCommandHandler>> _logger = new();

    private static readonly DateTime UtcNow = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    public SendReactivationNotificationCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
    }

    private SendReactivationNotificationCommandHandler CreateHandler() => new(
        _prefRepo.Object,
        _userRepo.Object,
        _subscriptionRepo.Object,
        _pushService.Object,
        _logRepo.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static NotificationPreference BuildPreference(Guid userId, bool pushEnabled = true, string? pushToken = "token-abc")
        => NotificationPreference.Create(userId, pushEnabled, pushToken, "granted", UtcNow);

    private static User BuildUser(string language = "pt-BR")
        => User.Create("hunter@awaken.app", "hash", "Hunter", language);

    private static Subscription BuildTrialExpired(Guid userId)
    {
        var s = Subscription.CreateTrial(userId, UtcNow.AddDays(-8), UtcNow.AddDays(-1));
        s.ExpireTrial();
        return s;
    }

    private static Subscription BuildSubscriptionExpired(Guid userId)
        => Subscription.CreateFromPaidPlan(userId, "monthly", "premium", "rc-123", UtcNow.AddDays(-1), UtcNow.AddDays(-1));

    private void SetupSinglePreference(NotificationPreference pref)
        => _prefRepo.Setup(r => r.GetAllWithPushEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([pref]);

    /// CA-001: trial expirado, consentimento, dentro da frequencia → envia.
    [Fact]
    public async Task CA001_TrialExpired_Eligible_Sends()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser();
        var subscription = BuildTrialExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
        result.Skipped.Should().Be(0);
        result.Eligible.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync("token-abc", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// CA-001: subscription_expired, consentimento, dentro da frequencia → envia.
    [Fact]
    public async Task CA001_SubscriptionExpired_Eligible_Sends()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser();
        var subscription = BuildSubscriptionExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
        result.Skipped.Should().Be(0);
    }

    /// CA-002: assinante ativo nao recebe reativacao.
    [Fact]
    public async Task CA002_ActiveSubscriber_DoesNotSend()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var subscription = Subscription.CreateFromPaidPlan(
            userId, "monthly", "premium", "rc-123", UtcNow.AddDays(30), UtcNow);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-004: frequencia minima de 7 dias nao atingida → nao envia.
    [Fact]
    public async Task RN004_FrequencyNotMet_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        // Enviou ha 3 dias — menos que 7 dias minimo.
        pref.RecordReactivationNotificationSent(UtcNow.AddDays(-3));

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _pushService.Verify(
            p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// RN-004: frequencia minima atingida (>= 7 dias) → envia.
    [Fact]
    public async Task RN004_FrequencyMet_Sends()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        // Enviou ha exatamente 7 dias.
        pref.RecordReactivationNotificationSent(UtcNow.AddDays(-7));
        var user = BuildUser();
        var subscription = BuildTrialExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(1);
    }

    /// RN-002: trial ativo nao recebe reativacao.
    [Fact]
    public async Task RN002_TrialActive_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var subscription = Subscription.CreateTrial(userId, UtcNow.AddDays(-2), UtcNow.AddDays(5));

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);

        var result = await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
    }

    /// RN-001: push desabilitado → nao envia.
    [Fact]
    public async Task RN001_PushDisabled_Skips()
    {
        var userId = Guid.NewGuid();
        var pref = NotificationPreference.Create(userId, false, null, "denied", UtcNow);

        SetupSinglePreference(pref);

        var result = await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
    }

    /// Push data payload deve incluir type=reactivation_notification e route=/subscription.
    [Fact]
    public async Task PushDataPayload_ContainsCorrectTypeAndRoute()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser();
        var subscription = BuildTrialExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        Dictionary<string, string>? capturedData = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, _, _, data, _) => capturedData = data);

        await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        capturedData.Should().ContainKey("type").WhoseValue.Should().Be("reactivation_notification");
        capturedData.Should().ContainKey("route").WhoseValue.Should().Be("/subscription");
    }

    /// Conteudo localizado en.
    [Fact]
    public async Task LocalizedContent_EN()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser("en");
        var subscription = BuildTrialExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        string? capturedTitle = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, _, _, _) => capturedTitle = title);

        await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        capturedTitle.Should().Be("Your journey awaits, Hunter!");
    }

    /// Conteudo localizado es.
    [Fact]
    public async Task LocalizedContent_ES()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser("es");
        var subscription = BuildTrialExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        string? capturedTitle = null;
        _pushService
            .Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, title, _, _, _) => capturedTitle = title);

        await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        capturedTitle.Should().Contain("espera");
    }

    /// Envia de reativacao persiste LastReactivationNotificationSentAt no preference.
    [Fact]
    public async Task AfterSend_RecordsReactivationTimestamp()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser();
        var subscription = BuildTrialExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        pref.LastReactivationNotificationSentAt.Should().Be(UtcNow);
        _prefRepo.Verify(r => r.Update(pref), Times.Once);
    }

    /// Falha no push registra decisao "failed" e nao quebra o job.
    [Fact]
    public async Task PushSendThrows_LogsFailedDecision()
    {
        var userId = Guid.NewGuid();
        var pref = BuildPreference(userId);
        var user = BuildUser();
        var subscription = BuildTrialExpired(userId);

        SetupSinglePreference(pref);
        _subscriptionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(subscription);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _pushService.Setup(p => p.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("push failed"));

        var result = await CreateHandler().Handle(new SendReactivationNotificationCommand(), CancellationToken.None);

        result.Sent.Should().Be(0);
        result.Skipped.Should().Be(1);
        _logRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(l =>
                l.UserId == userId &&
                l.NotificationType == "reactivation_notification" &&
                l.DecisionStatus == "failed" &&
                l.DecisionReason == "push_send_failed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
