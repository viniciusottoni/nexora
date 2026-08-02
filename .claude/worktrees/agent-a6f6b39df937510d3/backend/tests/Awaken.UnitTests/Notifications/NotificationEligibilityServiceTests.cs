using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Notifications;

public class NotificationEligibilityServiceTests
{
    private readonly Mock<INotificationPreferenceRepository> _prefRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISubscriptionRepository> _subRepo = new();
    private readonly Mock<INotificationLogRepository> _logRepo = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    private NotificationEligibilityService CreateService() => new(
        _prefRepo.Object,
        _userRepo.Object,
        _subRepo.Object,
        _logRepo.Object);

    private static NotificationPreference BuildPref(
        bool pushEnabled = true,
        string? token = "fcm-token",
        int dailyCount = 0,
        DateOnly? resetDate = null)
    {
        var pref = NotificationPreference.Create(UserId, pushEnabled, token, "granted", UtcNow);
        if (dailyCount > 0 && resetDate.HasValue)
        {
            var fakeUtc = resetDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            for (var i = 0; i < dailyCount; i++)
                pref.RecordNotificationSent(fakeUtc);
        }
        return pref;
    }

    private static User BuildActiveTrialUser()
    {
        var user = User.Create("h@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        return user;
    }

    private void SetupEligibleUser(NotificationPreference pref)
    {
        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildActiveTrialUser());
        _subRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _logRepo.Setup(r => r.GetTodayByUserIdAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    // RN-005: sem preferência salva → no_consent
    [Fact]
    public async Task RN005_NoPref_BlockedNoConsent()
    {
        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("no_consent");
    }

    // RN-005: PushEnabled=false → no_consent
    [Fact]
    public async Task RN005_PushDisabled_BlockedNoConsent()
    {
        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPref(pushEnabled: false, token: null));

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("no_consent");
    }

    // RN-005: PushToken null → no_consent
    [Fact]
    public async Task RN005_NullToken_BlockedNoConsent()
    {
        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPref(token: null));

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("no_consent");
    }

    // user_not_found
    [Fact]
    public async Task UserNotFound_Blocked()
    {
        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPref());
        _userRepo.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("user_not_found");
    }

    // RN-002: trial expirado para tipo non-reactivation → inactive_access
    [Fact]
    public async Task RN002_ExpiredTrial_BlockedInactiveAccess()
    {
        var user = User.Create("h@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(-1));

        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPref());
        _userRepo.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("inactive_access");
    }

    // RN-006: acesso ativo + tipo reactivation → active_access_for_reactivation
    [Fact]
    public async Task RN006_ActiveAccess_Reactivation_Blocked()
    {
        var pref = BuildPref();
        SetupEligibleUser(pref);

        var result = await CreateService().EvaluateAsync(UserId, "reactivation", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("active_access_for_reactivation");
    }

    // Redundância: mesmo tipo já enviado hoje
    [Fact]
    public async Task Redundant_SameTypeSentToday_Blocked()
    {
        var pref = BuildPref();
        var existingLog = NotificationLog.Create(
            UserId, "daily_quest_reminder", "sent", null, UtcNow.AddHours(-2));

        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildActiveTrialUser());
        _subRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _logRepo.Setup(r => r.GetTodayByUserIdAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingLog]);

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("redundant");
    }

    // RN-001/RN-003/RN-004: notificação de maior prioridade já enviada hoje bloqueia lembrete comum.
    [Fact]
    public async Task HigherPrioritySentToday_BlocksLowerPriority()
    {
        var pref = BuildPref();
        var existingLog = NotificationLog.Create(
            UserId, "streak_risk_alert", "sent", null, UtcNow.AddHours(-2));

        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pref);
        _userRepo.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildActiveTrialUser());
        _subRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _logRepo.Setup(r => r.GetTodayByUserIdAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingLog]);

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("higher_priority_already_sent");
    }

    // RN-002: limite diário atingido, tipo low-priority → daily_limit_reached
    [Fact]
    public async Task RN002_DailyLimitReached_LowPriority_Blocked()
    {
        var pref = BuildPref(dailyCount: 3, resetDate: Today);
        SetupEligibleUser(pref);

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Be("daily_limit_reached");
    }

    // RN-003/RN-004: limite diário atingido, tipo HIGH-priority (streak_risk_alert) → allowed (bypass)
    [Fact]
    public async Task RN003_RN004_DailyLimitReached_StreakRiskAlert_Allowed()
    {
        var pref = BuildPref(dailyCount: 3, resetDate: Today);
        SetupEligibleUser(pref);

        var result = await CreateService().EvaluateAsync(UserId, "streak_risk_alert", UtcNow);

        result.Allowed.Should().BeTrue();
        result.BlockReason.Should().BeNull();
    }

    // trial_expiring também é high priority → bypass do limite
    [Fact]
    public async Task TrialExpiring_DailyLimitReached_StillAllowed()
    {
        var pref = BuildPref(dailyCount: 3, resetDate: Today);
        SetupEligibleUser(pref);

        var result = await CreateService().EvaluateAsync(UserId, "trial_expiring", UtcNow);

        result.Allowed.Should().BeTrue();
    }

    // CA-001: caminho feliz — tudo elegível
    [Fact]
    public async Task CA001_AllEligible_Allowed()
    {
        var pref = BuildPref();
        SetupEligibleUser(pref);

        var result = await CreateService().EvaluateAsync(UserId, "daily_quest_reminder", UtcNow);

        result.Allowed.Should().BeTrue();
        result.BlockReason.Should().BeNull();
    }

    // reactivation permitida para usuário expirado
    [Fact]
    public async Task Reactivation_ExpiredUser_Allowed()
    {
        var user = User.Create("h@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(-5));

        _prefRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPref());
        _userRepo.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subRepo.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        _logRepo.Setup(r => r.GetTodayByUserIdAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateService().EvaluateAsync(UserId, "reactivation", UtcNow);

        result.Allowed.Should().BeTrue();
    }
}
