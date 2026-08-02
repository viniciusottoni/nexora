# US-095 — Evitar Notificações Excessivas — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar serviço central de elegibilidade de notificações com limite diário, priorização, bloqueio de redundância, auditoria via `NotificationLog` e endpoint `POST /internal/notifications/evaluate`.

**Architecture:** Novo aggregate `NotificationLog` no domínio persiste cada decisão de envio (sent/ignored/failed + motivo). `INotificationEligibilityService` encapsula todas as regras de negócio (consentimento, acesso, limite diário, prioridade, redundância). Handlers existentes (US-092, US-093) injetam `INotificationLogRepository` para registrar cada decisão; novo `EvaluateNotificationCommandHandler` usa o serviço central para avaliações pontuais por usuário.

**Tech Stack:** C# .NET 10, EF Core (PostgreSQL), MediatR, xUnit, FluentAssertions, Moq, Testcontainers.

---

## File Map

### Create
- `backend/src/Awaken.Domain/Entities/Notifications/NotificationLog.cs`
- `backend/src/Awaken.Domain/Repositories/INotificationLogRepository.cs`
- `backend/src/Awaken.Application/Common/Interfaces/INotificationEligibilityService.cs`
- `backend/src/Awaken.Application/Notifications/Commands/EvaluateNotification/EvaluateNotificationCommand.cs`
- `backend/src/Awaken.Application/Notifications/Commands/EvaluateNotification/EvaluateNotificationCommandHandler.cs`
- `backend/src/Awaken.Infrastructure/Persistence/Configurations/NotificationLogConfiguration.cs`
- `backend/src/Awaken.Infrastructure/Persistence/Repositories/NotificationLogRepository.cs`
- `backend/src/Awaken.Infrastructure/Services/NotificationEligibilityService.cs`
- `backend/tests/Awaken.UnitTests/Notifications/NotificationEligibilityServiceTests.cs`
- `backend/tests/Awaken.UnitTests/Notifications/EvaluateNotificationCommandHandlerTests.cs`
- `backend/tests/Awaken.IntegrationTests/EvaluateNotificationEndpointTests.cs`

### Modify
- `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs` — add `NotificationLogs` DbSet
- `backend/src/Awaken.Infrastructure/DependencyInjection.cs` — register `INotificationLogRepository`, `INotificationEligibilityService`
- `backend/src/Awaken.Api/Controllers/V1/InternalNotificationsController.cs` — add `POST /internal/notifications/evaluate`
- `backend/src/Awaken.Application/Notifications/Commands/SendDailyQuestReminder/SendDailyQuestReminderCommandHandler.cs` — inject `INotificationLogRepository`, log decisions
- `backend/src/Awaken.Application/Notifications/Commands/SendStreakRiskAlert/SendStreakRiskAlertCommandHandler.cs` — same

---

## Task 1: NotificationLog domain entity

**Files:**
- Create: `backend/src/Awaken.Domain/Entities/Notifications/NotificationLog.cs`

- [ ] **Step 1: Write the entity**

```csharp
using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Notifications;

/// US-095 RN-007: registra cada decisão de envio de notificação para auditoria básica.
public class NotificationLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; } = string.Empty;
    public string DecisionStatus { get; private set; } = string.Empty;
    public string? DecisionReason { get; private set; }
    public DateTime AttemptedAtUtc { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(
        Guid userId,
        string notificationType,
        string decisionStatus,
        string? decisionReason,
        DateTime utcNow) =>
        new()
        {
            UserId = userId,
            NotificationType = notificationType,
            DecisionStatus = decisionStatus,
            DecisionReason = decisionReason,
            AttemptedAtUtc = utcNow,
            CreatedAtUtc = utcNow,
        };
}
```

NotificationType values: `"daily_quest_reminder"`, `"streak_risk_alert"`, `"trial_expiring"`, `"reactivation"`
DecisionStatus values: `"sent"`, `"ignored"`, `"failed"`
DecisionReason values: `"daily_limit_reached"`, `"no_consent"`, `"inactive_access"`, `"redundant"`, `"quest_completed"`, `"no_streak"`, `"time_not_reached"`, `"user_not_found"`, `"active_access_for_reactivation"`

- [ ] **Step 2: Verify build**

```
cd backend/src && dotnet build Awaken.Domain/Awaken.Domain.csproj
```
Expected: Build succeeded.

---

## Task 2: INotificationLogRepository

**Files:**
- Create: `backend/src/Awaken.Domain/Repositories/INotificationLogRepository.cs`

- [ ] **Step 1: Write the interface**

```csharp
using Awaken.Domain.Common;
using Awaken.Domain.Entities.Notifications;

namespace Awaken.Domain.Repositories;

public interface INotificationLogRepository : IRepository<NotificationLog>
{
    Task<List<NotificationLog>> GetTodayByUserIdAsync(
        Guid userId,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Verify build**

```
cd backend/src && dotnet build Awaken.Domain/Awaken.Domain.csproj
```
Expected: Build succeeded.

---

## Task 3: EligibilityResult + INotificationEligibilityService

**Files:**
- Create: `backend/src/Awaken.Application/Common/Interfaces/INotificationEligibilityService.cs`

- [ ] **Step 1: Write the interface and result record**

```csharp
namespace Awaken.Application.Common.Interfaces;

public record EligibilityResult(bool Allowed, string? BlockReason)
{
    public static EligibilityResult Allow() => new(true, null);
    public static EligibilityResult Blocked(string reason) => new(false, reason);
}

public interface INotificationEligibilityService
{
    /// <summary>
    /// US-095: avalia se o usuário pode receber uma notificação do tipo informado.
    /// Verifica consentimento, acesso, redundância, limite diário e prioridade.
    /// NÃO persiste o resultado — isso é responsabilidade do chamador.
    /// </summary>
    Task<EligibilityResult> EvaluateAsync(
        Guid userId,
        string notificationType,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Verify build**

```
cd backend/src && dotnet build Awaken.Application/Awaken.Application.csproj
```
Expected: Build succeeded.

---

## Task 4: EvaluateNotificationCommand + Handler

**Files:**
- Create: `backend/src/Awaken.Application/Notifications/Commands/EvaluateNotification/EvaluateNotificationCommand.cs`
- Create: `backend/src/Awaken.Application/Notifications/Commands/EvaluateNotification/EvaluateNotificationCommandHandler.cs`

- [ ] **Step 1: Write the command and result**

`EvaluateNotificationCommand.cs`:
```csharp
using MediatR;

namespace Awaken.Application.Notifications.Commands.EvaluateNotification;

public record EvaluateNotificationCommand(Guid UserId, string NotificationType)
    : IRequest<EvaluateNotificationResult>;

public record EvaluateNotificationResult(bool Allowed, string? BlockReason, Guid LogId);
```

- [ ] **Step 2: Write the handler**

`EvaluateNotificationCommandHandler.cs`:
```csharp
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.EvaluateNotification;

/// US-095: avalia elegibilidade de notificação e registra decisão no NotificationLog.
public class EvaluateNotificationCommandHandler(
    INotificationEligibilityService eligibilityService,
    INotificationLogRepository notificationLogRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<EvaluateNotificationCommandHandler> logger)
    : IRequestHandler<EvaluateNotificationCommand, EvaluateNotificationResult>
{
    public async Task<EvaluateNotificationResult> Handle(
        EvaluateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;

        var result = await eligibilityService.EvaluateAsync(
            request.UserId,
            request.NotificationType,
            utcNow,
            cancellationToken);

        var decisionStatus = result.Allowed ? "sent" : "ignored";

        var log = NotificationLog.Create(
            request.UserId,
            request.NotificationType,
            decisionStatus,
            result.BlockReason,
            utcNow);

        await notificationLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // US-095: analytics events (ADR-015 — sem dados pessoais).
        if (!result.Allowed && result.BlockReason == "daily_limit_reached")
            logger.LogInformation(
                "notification_send_blocked_by_limit userId={UserId} type={Type}",
                request.UserId,
                request.NotificationType);

        logger.LogInformation(
            "notification_send_decision_logged logId={LogId} userId={UserId} type={Type} status={Status} reason={Reason}",
            log.Id,
            request.UserId,
            request.NotificationType,
            decisionStatus,
            result.BlockReason ?? "none");

        return new EvaluateNotificationResult(result.Allowed, result.BlockReason, log.Id);
    }
}
```

- [ ] **Step 3: Verify build**

```
cd backend/src && dotnet build Awaken.Application/Awaken.Application.csproj
```
Expected: Build succeeded.

---

## Task 5: NotificationLog EF Config + DbContext + Migration

**Files:**
- Create: `backend/src/Awaken.Infrastructure/Persistence/Configurations/NotificationLogConfiguration.cs`
- Modify: `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs`

- [ ] **Step 1: Write EF configuration**

`NotificationLogConfiguration.cs`:
```csharp
using Awaken.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId).IsRequired();

        builder.Property(l => l.NotificationType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(l => l.DecisionStatus)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(l => l.DecisionReason)
            .HasMaxLength(64);

        builder.Property(l => l.AttemptedAtUtc).IsRequired();

        builder.HasIndex(l => new { l.UserId, l.AttemptedAtUtc });
    }
}
```

- [ ] **Step 2: Add DbSet to AwakenDbContext**

In `AwakenDbContext.cs`, add after `NotificationPreferences`:
```csharp
public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
```

Also add using at top if needed:
```csharp
// already in Awaken.Domain.Entities.Notifications namespace — no new using required
```

- [ ] **Step 3: Generate EF migration**

```
cd backend/src && dotnet ef migrations add AddNotificationLog -p Awaken.Infrastructure -s Awaken.Api
```
Expected: migration file created in `Awaken.Infrastructure/Persistence/Migrations/`.

- [ ] **Step 4: Verify build**

```
cd backend/src && dotnet build
```
Expected: Build succeeded.

---

## Task 6: NotificationLogRepository

**Files:**
- Create: `backend/src/Awaken.Infrastructure/Persistence/Repositories/NotificationLogRepository.cs`

- [ ] **Step 1: Write the repository**

```csharp
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class NotificationLogRepository(AwakenDbContext context) : INotificationLogRepository
{
    public async Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.NotificationLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IEnumerable<NotificationLog>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.NotificationLogs.ToListAsync(cancellationToken);

    public async Task AddAsync(NotificationLog entity, CancellationToken cancellationToken = default) =>
        await context.NotificationLogs.AddAsync(entity, cancellationToken);

    public void Update(NotificationLog entity) => context.NotificationLogs.Update(entity);

    public void Remove(NotificationLog entity) => context.NotificationLogs.Remove(entity);

    public async Task<List<NotificationLog>> GetTodayByUserIdAsync(
        Guid userId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await context.NotificationLogs
            .Where(l => l.UserId == userId
                        && l.AttemptedAtUtc >= startOfDay
                        && l.AttemptedAtUtc <= endOfDay)
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 2: Verify build**

```
cd backend/src && dotnet build Awaken.Infrastructure/Awaken.Infrastructure.csproj
```
Expected: Build succeeded.

---

## Task 7: NotificationEligibilityService

**Files:**
- Create: `backend/src/Awaken.Infrastructure/Services/NotificationEligibilityService.cs`

- [ ] **Step 1: Write the service**

```csharp
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Repositories;

namespace Awaken.Infrastructure.Services;

/// US-095: serviço central de elegibilidade de notificação.
/// RN-001: verifica consentimento (push habilitado + token presente).
/// RN-002: verifica acesso ativo (trial ou assinatura).
/// RN-003/RN-004: tipos HIGH-priority (streak_risk_alert, trial_expiring) ignoram limite diário.
/// RN-005: usuário sem consentimento não recebe push.
/// RN-006: usuário com acesso ativo não recebe reactivation.
public class NotificationEligibilityService(
    INotificationPreferenceRepository notificationPreferenceRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    INotificationLogRepository notificationLogRepository)
    : INotificationEligibilityService
{
    private static readonly HashSet<string> HighPriorityTypes =
        ["streak_risk_alert", "trial_expiring"];

    public async Task<EligibilityResult> EvaluateAsync(
        Guid userId,
        string notificationType,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(utcNow);

        // RN-005: consentimento.
        var preference = await notificationPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        if (preference is null || !preference.PushEnabled || preference.PushToken is null)
            return EligibilityResult.Blocked("no_consent");

        // RN-002 / RN-006: verificação de acesso baseada no tipo.
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return EligibilityResult.Blocked("user_not_found");

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
        var accessStatus = subscription?.Plan is "monthly" or "annual"
            ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
            : user.ComputeAccessStatus(utcNow);

        bool hasActiveAccess = accessStatus is "trial_active" or "subscription_active";

        if (notificationType == "reactivation")
        {
            if (hasActiveAccess)
                return EligibilityResult.Blocked("active_access_for_reactivation");
        }
        else
        {
            if (!hasActiveAccess)
                return EligibilityResult.Blocked("inactive_access");
        }

        // Redundância: mesmo tipo já enviado hoje.
        var todayLogs = await notificationLogRepository.GetTodayByUserIdAsync(userId, today, cancellationToken);
        if (todayLogs.Any(l => l.NotificationType == notificationType && l.DecisionStatus == "sent"))
            return EligibilityResult.Blocked("redundant");

        // RN-002/RN-003/RN-004: limite diário — HIGH-priority ignora limite.
        bool isHighPriority = HighPriorityTypes.Contains(notificationType);
        if (!isHighPriority && !preference.CanReceiveNotificationToday(today))
            return EligibilityResult.Blocked("daily_limit_reached");

        return EligibilityResult.Allow();
    }
}
```

- [ ] **Step 2: Verify build**

```
cd backend/src && dotnet build Awaken.Infrastructure/Awaken.Infrastructure.csproj
```
Expected: Build succeeded.

---

## Task 8: DI Registration

**Files:**
- Modify: `backend/src/Awaken.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Register new services**

Add after the `INotificationPreferenceRepository` registration:
```csharp
services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
services.AddScoped<INotificationEligibilityService, NotificationEligibilityService>();
```

- [ ] **Step 2: Verify build**

```
cd backend/src && dotnet build
```
Expected: Build succeeded.

---

## Task 9: Endpoint /evaluate

**Files:**
- Modify: `backend/src/Awaken.Api/Controllers/V1/InternalNotificationsController.cs`

- [ ] **Step 1: Add evaluate endpoint**

Add using at top:
```csharp
using Awaken.Application.Notifications.Commands.EvaluateNotification;
```

Add action in controller body:
```csharp
[HttpPost("evaluate")]
public async Task<IActionResult> Evaluate(
    [FromBody] EvaluateNotificationRequest request,
    CancellationToken ct)
{
    var result = await mediator.Send(
        new EvaluateNotificationCommand(request.UserId, request.NotificationType), ct);
    return Ok(result);
}
```

- [ ] **Step 2: Create request DTO in Contracts**

Create `backend/src/Awaken.Contracts/Notifications/EvaluateNotificationRequest.cs`:
```csharp
namespace Awaken.Contracts.Notifications;

public record EvaluateNotificationRequest(Guid UserId, string NotificationType);
```

Add using to controller:
```csharp
using Awaken.Contracts.Notifications;
```

- [ ] **Step 3: Verify build**

```
cd backend/src && dotnet build
```
Expected: Build succeeded.

---

## Task 10: Update Existing Handlers — Log Decisions

**Files:**
- Modify: `backend/src/Awaken.Application/Notifications/Commands/SendDailyQuestReminder/SendDailyQuestReminderCommandHandler.cs`
- Modify: `backend/src/Awaken.Application/Notifications/Commands/SendStreakRiskAlert/SendStreakRiskAlertCommandHandler.cs`

### SendDailyQuestReminderCommandHandler

- [ ] **Step 1: Inject INotificationLogRepository**

Change constructor to add `INotificationLogRepository notificationLogRepository`:
```csharp
public class SendDailyQuestReminderCommandHandler(
    INotificationPreferenceRepository notificationPreferenceRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IQuestRepository questRepository,
    IPushNotificationService pushNotificationService,
    INotificationLogRepository notificationLogRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<SendDailyQuestReminderCommandHandler> logger)
```

- [ ] **Step 2: Add log call for each skip and each send**

Helper constant at top of class:
```csharp
private const string NotificationType = "daily_quest_reminder";
```

Replace each `skipped++; continue;` block with a pattern that also logs:
```csharp
// Example for daily limit skip:
if (!preference.CanReceiveNotificationToday(today))
{
    await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "daily_limit_reached", utcNow, cancellationToken);
    logger.LogInformation("notification_send_blocked_by_limit userId={UserId} type={Type}", preference.UserId, NotificationType);
    skipped++;
    continue;
}

// Example for preferred time skip:
if (preference.PreferredReminderTime.HasValue && localTimeOfDay < preference.PreferredReminderTime.Value)
{
    // No log needed for time_not_reached (not an audit-relevant skip per spec)
    skipped++;
    continue;
}

// Example for inactive access skip:
if (accessStatus is not ("trial_active" or "subscription_active"))
{
    await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "inactive_access", utcNow, cancellationToken);
    skipped++;
    continue;
}

// Example for quest completed skip:
if (quest?.Status == "completed")
{
    await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "quest_completed", utcNow, cancellationToken);
    skipped++;
    continue;
}
```

After successful push send, log sent:
```csharp
preference.RecordNotificationSent(utcNow);
notificationPreferenceRepository.Update(preference);
await LogDecisionAsync(notificationLogRepository, preference.UserId, "sent", null, utcNow, cancellationToken);
logger.LogInformation("notification_send_decision_logged userId={UserId} type={Type} status=sent", preference.UserId, NotificationType);
logger.LogInformation("daily_quest_reminder_sent userId={UserId}", preference.UserId);
sent++;
```

Add private helper at end of class:
```csharp
private static async Task LogDecisionAsync(
    INotificationLogRepository repo,
    Guid userId,
    string status,
    string? reason,
    DateTime utcNow,
    CancellationToken ct)
{
    var log = NotificationLog.Create(userId, NotificationType, status, reason, utcNow);
    await repo.AddAsync(log, ct);
}
```

Add using at top:
```csharp
using Awaken.Domain.Entities.Notifications;
```

### SendStreakRiskAlertCommandHandler

- [ ] **Step 3: Same pattern for StreakRiskAlert handler**

Add `INotificationLogRepository notificationLogRepository` to constructor, and:
```csharp
private const string NotificationType = "streak_risk_alert";
```

Log decisions for:
- `!preference.CanReceiveNotificationToday(today)` → `"ignored"`, `"daily_limit_reached"` + `notification_send_blocked_by_limit` log
- `progression is null || progression.CurrentStreakDays == 0` → `"ignored"`, `"no_streak"`
- `accessStatus is not (...)` → `"ignored"`, `"inactive_access"`
- `quest?.Status == "completed"` → `"ignored"`, `"quest_completed"`
- Successful send → `"sent"`, null reason + `notification_send_decision_logged` log

Same private helper `LogDecisionAsync` at bottom.

- [ ] **Step 4: Verify full build + existing tests**

```
cd backend && dotnet build src && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj
```
Expected: All tests pass. The existing handler tests may need a new `INotificationLogRepository` mock — see Task 11.

---

## Task 11: Unit Tests — NotificationEligibilityService

**Files:**
- Create: `backend/tests/Awaken.UnitTests/Notifications/NotificationEligibilityServiceTests.cs`

- [ ] **Step 1: Write all tests**

```csharp
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

    // RN-003/RN-004: limite diário atingido, mas tipo HIGH-priority → allowed (bypass)
    [Fact]
    public async Task RN003_RN004_DailyLimitReached_HighPriority_Allowed()
    {
        var pref = BuildPref(dailyCount: 3, resetDate: Today);
        SetupEligibleUser(pref);

        var result = await CreateService().EvaluateAsync(UserId, "streak_risk_alert", UtcNow);

        result.Allowed.Should().BeTrue();
        result.BlockReason.Should().BeNull();
    }

    // trial_expiring também é high priority
    [Fact]
    public async Task TrialExpiring_DailyLimitReached_StillAllowed()
    {
        var pref = BuildPref(dailyCount: 3, resetDate: Today);
        SetupEligibleUser(pref);

        var result = await CreateService().EvaluateAsync(UserId, "trial_expiring", UtcNow);

        result.Allowed.Should().BeTrue();
    }

    // CA-001 / CA-002: caminho feliz — tudo elegível
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
```

- [ ] **Step 2: Run tests**

```
cd backend && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "NotificationEligibilityServiceTests" -v normal
```
Expected: All tests pass.

---

## Task 12: Unit Tests — EvaluateNotificationCommandHandler

**Files:**
- Create: `backend/tests/Awaken.UnitTests/Notifications/EvaluateNotificationCommandHandlerTests.cs`

- [ ] **Step 1: Write tests**

```csharp
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

    // CA-001: allowed → logs "sent", returns allowed=true
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

    // CA-001: blocked by limit → logs "ignored" with reason, returns allowed=false
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

    // CA-002: any decision → saves and fires notification_send_decision_logged
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
}
```

- [ ] **Step 2: Run tests**

```
cd backend && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "EvaluateNotificationCommandHandlerTests" -v normal
```
Expected: All tests pass.

---

## Task 13: Update Existing Handler Unit Tests

**Files:**
- Modify: `backend/tests/Awaken.UnitTests/Notifications/SendDailyQuestReminderCommandHandlerTests.cs`
- Modify: `backend/tests/Awaken.UnitTests/Notifications/SendStreakRiskAlertCommandHandlerTests.cs`

- [ ] **Step 1: Add mock for INotificationLogRepository in SendDailyQuestReminderCommandHandlerTests**

Add field:
```csharp
private readonly Mock<INotificationLogRepository> _logRepo = new();
```

Update `CreateHandler()` to pass `_logRepo.Object` as the new parameter.

- [ ] **Step 2: Same for SendStreakRiskAlertCommandHandlerTests**

Add `private readonly Mock<INotificationLogRepository> _logRepo = new();` and pass to constructor.

- [ ] **Step 3: Run all unit tests**

```
cd backend && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj -v normal
```
Expected: All tests pass.

---

## Task 14: Integration Test — /evaluate endpoint

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/EvaluateNotificationEndpointTests.cs`

- [ ] **Step 1: Write integration tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Notifications;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

/// US-095: cobre POST /internal/notifications/evaluate contra Postgres real.
public class EvaluateNotificationEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:PostgreSQL", _postgres.GetConnectionString());
        });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.MigrateAsync();

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static async Task<Guid> SeedEligibleUserAsync(AwakenDbContext db)
    {
        var utcNow = DateTime.UtcNow;
        var user = User.Create($"{Guid.NewGuid():N}@awaken.app", "hash", "Hunter", "pt-BR");
        user.StartTrial(utcNow.AddDays(7));
        db.Users.Add(user);

        var pref = NotificationPreference.Create(user.Id, true, "fcm-eval-token", "granted", utcNow);
        db.NotificationPreferences.Add(pref);

        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Guid> SeedUserWithLimitReachedAsync(AwakenDbContext db)
    {
        var utcNow = DateTime.UtcNow;
        var user = User.Create($"{Guid.NewGuid():N}@awaken.app", "hash", "Hunter", "pt-BR");
        user.StartTrial(utcNow.AddDays(7));
        db.Users.Add(user);

        var pref = NotificationPreference.Create(user.Id, true, "fcm-limit-token", "granted", utcNow);
        // Fill daily limit (3 sends).
        pref.RecordNotificationSent(utcNow);
        pref.RecordNotificationSent(utcNow);
        pref.RecordNotificationSent(utcNow);
        db.NotificationPreferences.Add(pref);

        await db.SaveChangesAsync();
        return user.Id;
    }

    // CA-001: usuário elegível → allowed=true e log salvo
    [Fact]
    public async Task CA001_EligibleUser_ReturnsAllowed()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            userId = await SeedEligibleUserAsync(db);
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "daily_quest_reminder"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeTrue();
        body.BlockReason.Should().BeNull();
        body.LogId.Should().NotBeEmpty();

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await assertDb.NotificationLogs.SingleAsync(l => l.UserId == userId);
        log.DecisionStatus.Should().Be("sent");
        log.DecisionReason.Should().BeNull();
    }

    // CA-001: limite diário atingido → blocked por daily_limit_reached
    [Fact]
    public async Task CA001_DailyLimitReached_ReturnsBlocked()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            userId = await SeedUserWithLimitReachedAsync(db);
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "daily_quest_reminder"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeFalse();
        body.BlockReason.Should().Be("daily_limit_reached");

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var log = await assertDb.NotificationLogs.SingleAsync(l => l.UserId == userId);
        log.DecisionStatus.Should().Be("ignored");
        log.DecisionReason.Should().Be("daily_limit_reached");
    }

    // CA-002: limite atingido, mas streak_risk_alert (HIGH) → ainda permitido
    [Fact]
    public async Task CA002_HighPriority_BypassesLimit()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            userId = await SeedUserWithLimitReachedAsync(db);
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "streak_risk_alert"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeTrue();
    }

    // RN-005: usuário sem preferência → blocked no_consent
    [Fact]
    public async Task RN005_NoPreference_ReturnsBlockedNoConsent()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var user = User.Create($"{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
            user.StartTrial(DateTime.UtcNow.AddDays(7));
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        var response = await _client.PostAsJsonAsync("/internal/notifications/evaluate", new
        {
            userId,
            notificationType = "daily_quest_reminder"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        body!.Allowed.Should().BeFalse();
        body.BlockReason.Should().Be("no_consent");
    }

    private sealed record EvaluateResponse(bool Allowed, string? BlockReason, Guid LogId);
}
```

- [ ] **Step 2: Run integration tests**

```
cd backend && dotnet test tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj --filter "EvaluateNotificationEndpointTests" -v normal
```
Expected: All tests pass.

---

## Task 15: Full test suite

- [ ] **Step 1: Run all tests**

```
cd backend && dotnet test tests/ -v normal
```
Expected: All tests pass.

---

## Self-Review Checklist

### Spec coverage

| Requisito | Task |
|---|---|
| RN-001: evitar múltiplas notificações no mesmo dia | Task 7 (CanReceiveNotificationToday), Task 11 |
| RN-002: limite diário por usuário | Task 7, 11, 12, 14 |
| RN-003: trial tem prioridade sobre lembrete | Task 7 (HighPriorityTypes inclui trial_expiring), Task 11 |
| RN-004: streak tem prioridade sobre lembrete | Task 7, Task 11 |
| RN-005: sem consentimento → não recebe | Task 7, 11, 14 |
| RN-006: acesso ativo → não recebe reactivation | Task 7, 11 |
| RN-007: toda tentativa registra decisão | Task 4 (handler), Task 10 (handlers existentes) |
| CA-001: limite atingido → bloqueia | Task 11, 12, 14 |
| CA-002: streak vs lembrete → prioridade | Task 7, 11, 14 |
| Analytics: notification_send_blocked_by_limit | Task 4, 10 |
| Analytics: notification_send_decision_logged | Task 4, 10 |
| Endpoint POST /internal/notifications/evaluate | Task 9, 14 |
| NotificationLog entity + DB | Task 1, 5 |
| Idiomas: sem impacto Flutter nesta US | — (spec confirma: não exige tela) |

### Gaps encontrados
- Nenhum.

### Type consistency
- `EligibilityResult` definido em Task 3, usado em Task 7 e Task 12. ✓
- `EvaluateNotificationResult` definido em Task 4, retornado pelo handler em Task 4. ✓
- `NotificationLog.Create()` definido em Task 1, usado em Task 4 e Task 10. ✓
- `INotificationLogRepository.GetTodayByUserIdAsync()` definido em Task 2, implementado em Task 6, usado em Task 7. ✓
