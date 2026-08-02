# US-018 — Sincronizar Entitlement com RevenueCat — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose `POST /api/subscriptions/sync` on the backend and a `SyncEntitlementController` on Flutter so that RevenueCat entitlement data flows from the app → backend, keeping subscription status consistent.

**Architecture:** The Flutter app queries the RevenueCat SDK for active entitlements and posts them to the backend. The backend validates the payload, upserts the `Subscription` entity (adding RevenueCat-specific fields), and returns the resolved `accessStatus`. The existing `GET /api/subscriptions/status` is updated to also return `subscription_active` / `subscription_expired` when a paid plan is present.

**Tech Stack:** ASP.NET Core + MediatR + EF Core (backend); Flutter + Riverpod + purchases_flutter (frontend).

---

## File Map

### Backend — New
| File | Purpose |
|---|---|
| `backend/src/Awaken.Contracts/Subscriptions/SyncEntitlementRequest.cs` | Request DTO for sync command |
| `backend/src/Awaken.Contracts/Subscriptions/SyncEntitlementResponse.cs` | Response DTO for sync command |
| `backend/src/Awaken.Application/Subscriptions/Commands/SyncEntitlement/SyncEntitlementCommand.cs` | MediatR command record |
| `backend/src/Awaken.Application/Subscriptions/Commands/SyncEntitlement/SyncEntitlementCommandValidator.cs` | FluentValidation validator |
| `backend/src/Awaken.Application/Subscriptions/Commands/SyncEntitlement/SyncEntitlementCommandHandler.cs` | Handler: upserts Subscription |
| `backend/tests/Awaken.UnitTests/Domain/SubscriptionTests.cs` | Domain unit tests for new entity methods |
| `backend/tests/Awaken.UnitTests/Subscriptions/SyncEntitlementCommandHandlerTests.cs` | Unit tests for handler |
| `backend/tests/Awaken.IntegrationTests/SubscriptionsSyncEndpointTests.cs` | Integration tests for sync endpoint |

### Backend — Modified
| File | Change |
|---|---|
| `backend/src/Awaken.Domain/Entities/Subscriptions/Subscription.cs` | Add RC fields, `CreateFromPaidPlan`, `ActivatePaidPlan`, `MarkExpired` |
| `backend/src/Awaken.Contracts/Subscriptions/SubscriptionStatusResponse.cs` | Add optional `Plan`, `ExpiresAt` |
| `backend/src/Awaken.Application/Subscriptions/Queries/GetSubscriptionStatus/GetSubscriptionStatusQueryHandler.cs` | Handle paid plan priority |
| `backend/src/Awaken.Api/Controllers/V1/SubscriptionsController.cs` | Add `POST /api/subscriptions/sync` |
| `backend/src/Awaken.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs` | Map new columns |
| (EF migration generated at runtime) | |

### Flutter — New
| File | Purpose |
|---|---|
| `apps/mobile/lib/features/subscriptions/data/dtos/sync_entitlement_request_dto.dart` | Request payload DTO |
| `apps/mobile/lib/features/subscriptions/data/dtos/sync_entitlement_response_dto.dart` | Response DTO |
| `apps/mobile/lib/features/subscriptions/data/services/revenue_cat_service.dart` | Abstract + real RevenueCat adapter |
| `apps/mobile/lib/features/subscriptions/presentation/providers/sync_entitlement_state.dart` | Sealed state classes |
| `apps/mobile/lib/features/subscriptions/presentation/providers/sync_entitlement_controller.dart` | Notifier: orchestrates RC → backend → session |
| `apps/mobile/test/features/subscriptions/presentation/providers/sync_entitlement_controller_test.dart` | Unit tests |

### Flutter — Modified
| File | Change |
|---|---|
| `apps/mobile/lib/features/subscriptions/data/dtos/subscription_status_response_dto.dart` | Add `plan`, `expiresAt` |
| `apps/mobile/lib/features/subscriptions/data/datasources/subscription_remote_data_source.dart` | Add `syncEntitlement` |
| `apps/mobile/lib/features/subscriptions/domain/repositories/subscription_repository.dart` | Add `syncEntitlement` |
| `apps/mobile/lib/features/subscriptions/data/repositories/subscription_repository_impl.dart` | Implement `syncEntitlement` |
| `apps/mobile/lib/features/subscriptions/presentation/providers/subscription_providers.dart` | Add `revenueCatServiceProvider` |
| `apps/mobile/lib/features/subscriptions/presentation/providers/subscription_status_state.dart` | Add `isSubscriptionActive`, `isSubscriptionExpired` |
| `apps/mobile/lib/l10n/app_pt.arb` | Sync error keys |
| `apps/mobile/lib/l10n/app_en.arb` | Sync error keys |
| `apps/mobile/lib/l10n/app_es.arb` | Sync error keys |
| `apps/mobile/test/features/subscriptions/data/datasources/subscription_remote_data_source_test.dart` | Add sync test group |
| `apps/mobile/test/features/subscriptions/presentation/providers/subscription_status_controller_test.dart` | Add subscription_active test |

---

## Task 1: Backend Domain — Subscription entity + Contracts

**Files:**
- Modify: `backend/src/Awaken.Domain/Entities/Subscriptions/Subscription.cs`
- Modify: `backend/src/Awaken.Contracts/Subscriptions/SubscriptionStatusResponse.cs`
- Create: `backend/src/Awaken.Contracts/Subscriptions/SyncEntitlementRequest.cs`
- Create: `backend/src/Awaken.Contracts/Subscriptions/SyncEntitlementResponse.cs`

- [ ] **Step 1.1: Replace `Subscription.cs` with extended entity**

Replace the entire file content:

```csharp
using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Subscriptions;

public class Subscription : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Plan { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTime? TrialStartedAt { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? TrialConsumedAt { get; private set; }
    public string? Entitlement { get; private set; }
    public string? RevenueCatCustomerId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastRevenueCatSyncAt { get; private set; }

    private Subscription() { }

    public static Subscription CreateTrial(Guid userId, DateTime trialStartedAt, DateTime trialEndsAt)
    {
        return new Subscription
        {
            UserId = userId,
            Plan = "trial",
            Status = "trial_active",
            TrialStartedAt = trialStartedAt,
            TrialEndsAt = trialEndsAt,
        };
    }

    public static Subscription CreateFromPaidPlan(
        Guid userId, string plan, string entitlement,
        string revenueCatCustomerId, DateTime expiresAt, DateTime syncedAt)
    {
        return new Subscription
        {
            UserId = userId,
            Plan = plan,
            Status = expiresAt > syncedAt ? "subscription_active" : "subscription_expired",
            Entitlement = entitlement,
            RevenueCatCustomerId = revenueCatCustomerId,
            ExpiresAt = expiresAt,
            LastRevenueCatSyncAt = syncedAt,
        };
    }

    public bool ExpireTrial()
    {
        if (Status == "trial_expired") return false;
        Status = "trial_expired";
        return true;
    }

    public void ActivatePaidPlan(
        string plan, string entitlement, string revenueCatCustomerId,
        DateTime expiresAt, DateTime syncedAt)
    {
        Plan = plan;
        Entitlement = entitlement;
        RevenueCatCustomerId = revenueCatCustomerId;
        ExpiresAt = expiresAt;
        LastRevenueCatSyncAt = syncedAt;
        Status = expiresAt > syncedAt ? "subscription_active" : "subscription_expired";
        UpdatedAtUtc = syncedAt;
    }

    public bool MarkExpired(DateTime syncedAt)
    {
        if (Status == "subscription_expired") return false;
        Status = "subscription_expired";
        LastRevenueCatSyncAt = syncedAt;
        UpdatedAtUtc = syncedAt;
        return true;
    }
}
```

- [ ] **Step 1.2: Update `SubscriptionStatusResponse.cs` to add Plan and ExpiresAt**

```csharp
namespace Awaken.Contracts.Subscriptions;

public record SubscriptionStatusResponse(
    string AccessStatus,
    DateTime? TrialStartedAt,
    DateTime? TrialEndsAt,
    int? DaysRemaining,
    string? Plan = null,
    DateTime? ExpiresAt = null);
```

- [ ] **Step 1.3: Create `SyncEntitlementRequest.cs`**

```csharp
namespace Awaken.Contracts.Subscriptions;

public record SyncEntitlementRequest(
    string RevenueCatCustomerId,
    string Entitlement,
    string Plan,
    DateTime ExpiresAt);
```

- [ ] **Step 1.4: Create `SyncEntitlementResponse.cs`**

```csharp
namespace Awaken.Contracts.Subscriptions;

public record SyncEntitlementResponse(
    string AccessStatus,
    string Plan,
    DateTime ExpiresAt);
```

- [ ] **Step 1.5: Build to verify no compile errors**

Run: `dotnet build backend/src/Awaken.Domain backend/src/Awaken.Contracts`
Expected: Build succeeded

---

## Task 2: Backend Application — SyncEntitlementCommand + Handler + Status update

**Files:**
- Create: `backend/src/Awaken.Application/Subscriptions/Commands/SyncEntitlement/SyncEntitlementCommand.cs`
- Create: `backend/src/Awaken.Application/Subscriptions/Commands/SyncEntitlement/SyncEntitlementCommandValidator.cs`
- Create: `backend/src/Awaken.Application/Subscriptions/Commands/SyncEntitlement/SyncEntitlementCommandHandler.cs`
- Modify: `backend/src/Awaken.Application/Subscriptions/Queries/GetSubscriptionStatus/GetSubscriptionStatusQueryHandler.cs`

- [ ] **Step 2.1: Create `SyncEntitlementCommand.cs`**

```csharp
using Awaken.Contracts.Subscriptions;
using MediatR;

namespace Awaken.Application.Subscriptions.Commands.SyncEntitlement;

public record SyncEntitlementCommand(
    string RevenueCatCustomerId,
    string Entitlement,
    string Plan,
    DateTime ExpiresAt
) : IRequest<SyncEntitlementResponse>;
```

- [ ] **Step 2.2: Create `SyncEntitlementCommandValidator.cs`**

```csharp
using FluentValidation;

namespace Awaken.Application.Subscriptions.Commands.SyncEntitlement;

public class SyncEntitlementCommandValidator : AbstractValidator<SyncEntitlementCommand>
{
    public SyncEntitlementCommandValidator()
    {
        RuleFor(x => x.RevenueCatCustomerId).NotEmpty();
        RuleFor(x => x.Entitlement).NotEmpty();
        RuleFor(x => x.Plan)
            .NotEmpty()
            .Must(p => p == "monthly" || p == "annual")
            .WithMessage("Plan must be 'monthly' or 'annual'.");
    }
}
```

- [ ] **Step 2.3: Create `SyncEntitlementCommandHandler.cs`**

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Subscriptions.Commands.SyncEntitlement;

public class SyncEntitlementCommandHandler(
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<SyncEntitlementCommand, SyncEntitlementResponse>
{
    public async Task<SyncEntitlementResponse> Handle(
        SyncEntitlementCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        var utcNow = dateTimeService.UtcNow;
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);

        if (subscription is null)
        {
            subscription = Subscription.CreateFromPaidPlan(
                userId, request.Plan, request.Entitlement,
                request.RevenueCatCustomerId, request.ExpiresAt, utcNow);
            await subscriptionRepository.AddAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.ActivatePaidPlan(
                request.Plan, request.Entitlement,
                request.RevenueCatCustomerId, request.ExpiresAt, utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessStatus = request.ExpiresAt > utcNow
            ? "subscription_active"
            : "subscription_expired";

        return new SyncEntitlementResponse(accessStatus, request.Plan, request.ExpiresAt);
    }
}
```

- [ ] **Step 2.4: Update `GetSubscriptionStatusQueryHandler.cs` to handle paid plans**

Replace the entire file:

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Subscriptions.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusQueryHandler(
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusResponse>
{
    public async Task<SubscriptionStatusResponse> Handle(
        GetSubscriptionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        var utcNow = dateTimeService.UtcNow;
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);

        // Paid plan takes priority over trial
        if (subscription?.Plan is "monthly" or "annual")
        {
            var isActive = subscription.ExpiresAt > utcNow;
            var status = isActive ? "subscription_active" : "subscription_expired";

            if (!isActive && subscription.MarkExpired(utcNow))
                await unitOfWork.SaveChangesAsync(cancellationToken);

            int? daysRemaining = null;
            if (isActive && subscription.ExpiresAt is not null)
            {
                var remaining = (subscription.ExpiresAt.Value - utcNow).TotalDays;
                daysRemaining = (int)Math.Ceiling(remaining);
                if (daysRemaining < 0) daysRemaining = 0;
            }

            return new SubscriptionStatusResponse(
                status, null, null, daysRemaining, subscription.Plan, subscription.ExpiresAt);
        }

        // Trial logic
        var accessStatus = user.ComputeAccessStatus(utcNow);

        if (accessStatus == "trial_expired" && subscription is not null)
        {
            if (subscription.ExpireTrial())
                await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        int? trialDaysRemaining = null;
        if (subscription?.TrialEndsAt is not null && accessStatus == "trial_active")
        {
            var remaining = (subscription.TrialEndsAt.Value - utcNow).TotalDays;
            trialDaysRemaining = (int)Math.Ceiling(remaining);
            if (trialDaysRemaining < 0) trialDaysRemaining = 0;
        }

        return new SubscriptionStatusResponse(
            accessStatus,
            subscription?.TrialStartedAt,
            subscription?.TrialEndsAt,
            trialDaysRemaining);
    }
}
```

- [ ] **Step 2.5: Build to verify**

Run: `dotnet build backend/src/Awaken.Application`
Expected: Build succeeded

---

## Task 3: Backend API endpoint + Infrastructure + EF Migration

**Files:**
- Modify: `backend/src/Awaken.Api/Controllers/V1/SubscriptionsController.cs`
- Modify: `backend/src/Awaken.Infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs`

- [ ] **Step 3.1: Add sync endpoint to `SubscriptionsController.cs`**

```csharp
using Awaken.Application.Subscriptions.Commands.StartTrial;
using Awaken.Application.Subscriptions.Commands.SyncEntitlement;
using Awaken.Application.Subscriptions.Queries.GetSubscriptionStatus;
using Awaken.Contracts.Subscriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Awaken.Api.Controllers.V1;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController(IMediator mediator) : ControllerBase
{
    [HttpPost("trial/start")]
    public async Task<IActionResult> StartTrial(CancellationToken ct)
    {
        var result = await mediator.Send(new StartTrialCommand(), ct);
        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await mediator.Send(new GetSubscriptionStatusQuery(), ct);
        return Ok(result);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncEntitlementRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new SyncEntitlementCommand(
            request.RevenueCatCustomerId,
            request.Entitlement,
            request.Plan,
            request.ExpiresAt), ct);
        return Ok(result);
    }
}
```

- [ ] **Step 3.2: Update `SubscriptionConfiguration.cs` to map new fields**

```csharp
using Awaken.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Plan)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(s => s.Entitlement)
            .HasMaxLength(128);

        builder.Property(s => s.RevenueCatCustomerId)
            .HasMaxLength(256);

        builder.HasIndex(s => s.UserId);
    }
}
```

- [ ] **Step 3.3: Run EF Core migration**

Run from `backend/`:
```
dotnet ef migrations add AddRevenueCatFieldsToSubscription -p src/Awaken.Infrastructure -s src/Awaken.Api
```
Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 3.4: Full backend build**

Run: `dotnet build backend/`
Expected: Build succeeded, 0 errors

---

## Task 4: Backend Unit Tests

**Files:**
- Create: `backend/tests/Awaken.UnitTests/Domain/SubscriptionTests.cs`
- Create: `backend/tests/Awaken.UnitTests/Subscriptions/SyncEntitlementCommandHandlerTests.cs`
- Modify: `backend/tests/Awaken.UnitTests/Subscriptions/GetSubscriptionStatusQueryHandlerTests.cs`

- [ ] **Step 4.1: Create `SubscriptionTests.cs`**

```csharp
using Awaken.Domain.Entities.Subscriptions;
using FluentAssertions;

namespace Awaken.UnitTests.Domain;

public class SubscriptionTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateFromPaidPlan_SetsSubscriptionActiveWhenExpiresInFuture()
    {
        var expiresAt = _utcNow.AddDays(30);

        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_customer_123", expiresAt, _utcNow);

        subscription.UserId.Should().Be(_userId);
        subscription.Plan.Should().Be("monthly");
        subscription.Status.Should().Be("subscription_active");
        subscription.Entitlement.Should().Be("pro_access");
        subscription.RevenueCatCustomerId.Should().Be("rc_customer_123");
        subscription.ExpiresAt.Should().Be(expiresAt);
        subscription.LastRevenueCatSyncAt.Should().Be(_utcNow);
    }

    [Fact]
    public void CreateFromPaidPlan_SetsSubscriptionExpiredWhenExpiresInPast()
    {
        var expiresAt = _utcNow.AddDays(-1);

        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "annual", "pro_access", "rc_customer_123", expiresAt, _utcNow);

        subscription.Status.Should().Be("subscription_expired");
    }

    [Fact]
    public void ActivatePaidPlan_UpdatesAllFieldsAndSetsActive()
    {
        var trialSubscription = Subscription.CreateTrial(_userId, _utcNow.AddDays(-8), _utcNow.AddDays(-1));
        var expiresAt = _utcNow.AddDays(30);

        trialSubscription.ActivatePaidPlan("monthly", "pro_access", "rc_123", expiresAt, _utcNow);

        trialSubscription.Plan.Should().Be("monthly");
        trialSubscription.Status.Should().Be("subscription_active");
        trialSubscription.Entitlement.Should().Be("pro_access");
        trialSubscription.RevenueCatCustomerId.Should().Be("rc_123");
        trialSubscription.ExpiresAt.Should().Be(expiresAt);
        trialSubscription.LastRevenueCatSyncAt.Should().Be(_utcNow);
    }

    [Fact]
    public void ActivatePaidPlan_SetsExpiredWhenExpiresInPast()
    {
        var subscription = Subscription.CreateTrial(_userId, _utcNow.AddDays(-8), _utcNow.AddDays(-1));
        var expiresAt = _utcNow.AddDays(-2);

        subscription.ActivatePaidPlan("monthly", "pro_access", "rc_123", expiresAt, _utcNow);

        subscription.Status.Should().Be("subscription_expired");
    }

    [Fact]
    public void MarkExpired_ReturnsTrueAndSetsStatusWhenNotAlreadyExpired()
    {
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_123", _utcNow.AddDays(1), _utcNow);

        var result = subscription.MarkExpired(_utcNow);

        result.Should().BeTrue();
        subscription.Status.Should().Be("subscription_expired");
        subscription.LastRevenueCatSyncAt.Should().Be(_utcNow);
    }

    [Fact]
    public void MarkExpired_ReturnsFalseWhenAlreadyExpired()
    {
        var subscription = Subscription.CreateFromPaidPlan(
            _userId, "monthly", "pro_access", "rc_123", _utcNow.AddDays(-1), _utcNow);

        var result = subscription.MarkExpired(_utcNow);

        result.Should().BeFalse();
        subscription.Status.Should().Be("subscription_expired");
    }
}
```

- [ ] **Step 4.2: Run domain tests to confirm they pass**

Run: `dotnet test backend/tests/Awaken.UnitTests --filter "FullyQualifiedName~SubscriptionTests"`
Expected: 5 passed

- [ ] **Step 4.3: Create `SyncEntitlementCommandHandlerTests.cs`**

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Subscriptions.Commands.SyncEntitlement;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Subscriptions;

public class SyncEntitlementCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _expiresAt;

    public SyncEntitlementCommandHandlerTests()
    {
        _expiresAt = _utcNow.AddDays(30);
        _currentUserService.Setup(s => s.UserId).Returns(_userId);
        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
    }

    private SyncEntitlementCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _subscriptionRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private SyncEntitlementCommand BuildCommand(string plan = "monthly", DateTime? expiresAt = null) =>
        new("rc_customer_123", "pro_access", plan, expiresAt ?? _expiresAt);

    [Fact]
    public async Task HandleCreatesNewSubscriptionWhenNoneExists()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.Plan.Should().Be("monthly");
        result.ExpiresAt.Should().Be(_expiresAt);
        _subscriptionRepository.Verify(r => r.AddAsync(
            It.Is<Subscription>(s =>
                s.UserId == _userId &&
                s.Plan == "monthly" &&
                s.Status == "subscription_active" &&
                s.Entitlement == "pro_access" &&
                s.RevenueCatCustomerId == "rc_customer_123"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleActivatesPaidPlanOnExistingTrialSubscription()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(_utcNow.AddDays(-1));
        var existing = Subscription.CreateTrial(_userId, _utcNow.AddDays(-8), _utcNow.AddDays(-1));

        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(BuildCommand("annual"), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_active");
        result.Plan.Should().Be("annual");
        existing.Plan.Should().Be("annual");
        existing.Status.Should().Be("subscription_active");
        existing.Entitlement.Should().Be("pro_access");
        _subscriptionRepository.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleReturnsExpiredWhenExpiresAtIsInPast()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var pastExpiry = _utcNow.AddDays(-5);
        var result = await CreateHandler().Handle(BuildCommand(expiresAt: pastExpiry), CancellationToken.None);

        result.AccessStatus.Should().Be("subscription_expired");
    }

    [Fact]
    public async Task HandleThrowsUnauthorizedWhenUserNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(BuildCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 4.4: Add subscription_active tests to `GetSubscriptionStatusQueryHandlerTests.cs`**

Append these test methods to the existing `GetSubscriptionStatusQueryHandlerTests` class:

```csharp
[Fact]
public async Task HandleReturnsSubscriptionActiveWhenPaidPlanNotExpired()
{
    var expiresAt = _utcNow.AddDays(30);
    var user = User.Create("hunter@awaken.app", "hash", "Hunter");
    var subscription = Subscription.CreateFromPaidPlan(_userId, "monthly", "pro_access", "rc_123", expiresAt, _utcNow);

    _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(user);
    _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(subscription);

    var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

    result.AccessStatus.Should().Be("subscription_active");
    result.Plan.Should().Be("monthly");
    result.ExpiresAt.Should().Be(expiresAt);
    result.TrialStartedAt.Should().BeNull();
    _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}

[Fact]
public async Task HandleReturnsSubscriptionExpiredAndMarksItWhenPaidPlanExpired()
{
    var expiresAt = _utcNow.AddDays(-5);
    var user = User.Create("hunter@awaken.app", "hash", "Hunter");
    var subscription = Subscription.CreateFromPaidPlan(_userId, "annual", "pro_access", "rc_123", expiresAt, _utcNow.AddDays(-6));

    _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(user);
    _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(subscription);

    var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

    result.AccessStatus.Should().Be("subscription_expired");
    result.Plan.Should().Be("annual");
    subscription.Status.Should().Be("subscription_expired");
    _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task HandlePaidPlanTakesPriorityOverExpiredTrial()
{
    var expiresAt = _utcNow.AddDays(15);
    var user = User.Create("hunter@awaken.app", "hash", "Hunter");
    user.StartTrial(_utcNow.AddDays(-1)); // trial expired
    var subscription = Subscription.CreateTrial(_userId, _utcNow.AddDays(-8), _utcNow.AddDays(-1));
    subscription.ActivatePaidPlan("monthly", "pro_access", "rc_123", expiresAt, _utcNow);

    _userRepository.Setup(r => r.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(user);
    _subscriptionRepository.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(subscription);

    var result = await CreateHandler().Handle(new GetSubscriptionStatusQuery(), CancellationToken.None);

    result.AccessStatus.Should().Be("subscription_active");
    result.Plan.Should().Be("monthly");
}
```

- [ ] **Step 4.5: Run all unit tests**

Run: `dotnet test backend/tests/Awaken.UnitTests`
Expected: All tests pass (including new ones)

---

## Task 5: Backend Integration Tests

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/SubscriptionsSyncEndpointTests.cs`

- [ ] **Step 5.1: Create `SubscriptionsSyncEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Contracts.Subscriptions;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class SubscriptionsSyncEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndGetTokenAsync(string email = "hunter@awaken.app")
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task SyncReturnsSubscriptionActiveForFutureExpiry()
    {
        var token = await RegisterAndGetTokenAsync("sync_active@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        var payload = new SyncEntitlementRequest("rc_customer_001", "pro_access", "monthly", expiresAt);

        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
        body.Plan.Should().Be("monthly");
        body.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SyncReturnsSubscriptionExpiredForPastExpiry()
    {
        var token = await RegisterAndGetTokenAsync("sync_expired@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-5);
        var payload = new SyncEntitlementRequest("rc_customer_002", "pro_access", "annual", expiresAt);

        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("subscription_expired");
    }

    [Fact]
    public async Task SyncIsIdempotentWhenCalledMultipleTimes()
    {
        var token = await RegisterAndGetTokenAsync("sync_idempotent@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(30);
        var payload = new SyncEntitlementRequest("rc_customer_003", "pro_access", "monthly", expiresAt);

        var first = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);
        var second = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await second.Content.ReadFromJsonAsync<SyncEntitlementResponse>();
        body!.AccessStatus.Should().Be("subscription_active");
    }

    [Fact]
    public async Task SyncAfterTrialSetsSubscriptionActiveAndStatusReflectsIt()
    {
        var token = await RegisterAndGetTokenAsync("sync_after_trial@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Start trial first
        await _client.PostAsync("/api/subscriptions/trial/start", null);

        // Then sync paid plan
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var syncPayload = new SyncEntitlementRequest("rc_customer_004", "pro_access", "annual", expiresAt);
        var syncResponse = await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);
        syncResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Status endpoint should now return subscription_active
        var statusResponse = await _client.GetAsync("/api/subscriptions/status");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await statusResponse.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        status!.AccessStatus.Should().Be("subscription_active");
        status.Plan.Should().Be("annual");
    }

    [Fact]
    public async Task SyncReturnsBadRequestWhenPlanIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("sync_invalid@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new SyncEntitlementRequest("rc_customer_005", "pro_access", "invalid_plan", DateTime.UtcNow.AddDays(30));

        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task SyncReturnsUnauthorizedWhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var payload = new SyncEntitlementRequest("rc_customer_006", "pro_access", "monthly", DateTime.UtcNow.AddDays(30));

        var response = await _client.PostAsJsonAsync("/api/subscriptions/sync", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 5.2: Run integration tests**

Run: `dotnet test backend/tests/Awaken.IntegrationTests --filter "FullyQualifiedName~SubscriptionsSyncEndpointTests"`
Expected: 6 passed

---

## Task 6: Flutter Data Layer

**Files:**
- Create: `apps/mobile/lib/features/subscriptions/data/dtos/sync_entitlement_request_dto.dart`
- Create: `apps/mobile/lib/features/subscriptions/data/dtos/sync_entitlement_response_dto.dart`
- Create: `apps/mobile/lib/features/subscriptions/data/services/revenue_cat_service.dart`
- Modify: `apps/mobile/lib/features/subscriptions/data/dtos/subscription_status_response_dto.dart`
- Modify: `apps/mobile/lib/features/subscriptions/data/datasources/subscription_remote_data_source.dart`
- Modify: `apps/mobile/lib/features/subscriptions/domain/repositories/subscription_repository.dart`
- Modify: `apps/mobile/lib/features/subscriptions/data/repositories/subscription_repository_impl.dart`

- [ ] **Step 6.1: Create `sync_entitlement_request_dto.dart`**

```dart
class SyncEntitlementRequestDto {
  const SyncEntitlementRequestDto({
    required this.revenueCatCustomerId,
    required this.entitlement,
    required this.plan,
    required this.expiresAt,
  });

  final String revenueCatCustomerId;
  final String entitlement;
  final String plan;
  final DateTime expiresAt;

  Map<String, dynamic> toJson() => {
        'revenueCatCustomerId': revenueCatCustomerId,
        'entitlement': entitlement,
        'plan': plan,
        'expiresAt': expiresAt.toUtc().toIso8601String(),
      };
}
```

- [ ] **Step 6.2: Create `sync_entitlement_response_dto.dart`**

```dart
class SyncEntitlementResponseDto {
  const SyncEntitlementResponseDto({
    required this.accessStatus,
    required this.plan,
    required this.expiresAt,
  });

  final String accessStatus;
  final String plan;
  final DateTime expiresAt;

  factory SyncEntitlementResponseDto.fromJson(Map<String, dynamic> json) =>
      SyncEntitlementResponseDto(
        accessStatus: json['accessStatus'] as String,
        plan: json['plan'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String),
      );
}
```

- [ ] **Step 6.3: Update `subscription_status_response_dto.dart` to add `plan` and `expiresAt`**

```dart
class SubscriptionStatusResponseDto {
  const SubscriptionStatusResponseDto({
    required this.accessStatus,
    this.trialStartedAt,
    this.trialEndsAt,
    this.daysRemaining,
    this.plan,
    this.expiresAt,
  });

  final String accessStatus;
  final DateTime? trialStartedAt;
  final DateTime? trialEndsAt;
  final int? daysRemaining;
  final String? plan;
  final DateTime? expiresAt;

  factory SubscriptionStatusResponseDto.fromJson(Map<String, dynamic> json) =>
      SubscriptionStatusResponseDto(
        accessStatus: json['accessStatus'] as String,
        trialStartedAt: json['trialStartedAt'] != null
            ? DateTime.parse(json['trialStartedAt'] as String)
            : null,
        trialEndsAt: json['trialEndsAt'] != null
            ? DateTime.parse(json['trialEndsAt'] as String)
            : null,
        daysRemaining: json['daysRemaining'] as int?,
        plan: json['plan'] as String?,
        expiresAt: json['expiresAt'] != null
            ? DateTime.parse(json['expiresAt'] as String)
            : null,
      );
}
```

- [ ] **Step 6.4: Update `subscription_remote_data_source.dart` to add `syncEntitlement`**

```dart
import 'package:dio/dio.dart';
import '../../../../core/errors/app_error.dart';
import '../dtos/start_trial_response_dto.dart';
import '../dtos/subscription_status_response_dto.dart';
import '../dtos/sync_entitlement_request_dto.dart';
import '../dtos/sync_entitlement_response_dto.dart';

class SubscriptionRemoteDataSource {
  const SubscriptionRemoteDataSource(this._dio);
  final Dio _dio;

  Future<StartTrialResponseDto> startTrial() async {
    try {
      final response = await _dio.post('/api/subscriptions/trial/start');
      return StartTrialResponseDto.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      if (e.response?.statusCode == 409) throw const TrialAlreadyUsedError();
      if (e.type == DioExceptionType.connectionTimeout ||
          e.type == DioExceptionType.sendTimeout ||
          e.type == DioExceptionType.receiveTimeout ||
          e.type == DioExceptionType.connectionError) {
        throw const NetworkError();
      }
      throw const UnexpectedError();
    }
  }

  Future<SubscriptionStatusResponseDto> getStatus() async {
    try {
      final response = await _dio.get('/api/subscriptions/status');
      return SubscriptionStatusResponseDto.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      if (e.type == DioExceptionType.connectionTimeout ||
          e.type == DioExceptionType.sendTimeout ||
          e.type == DioExceptionType.receiveTimeout ||
          e.type == DioExceptionType.connectionError) {
        throw const NetworkError();
      }
      throw const UnexpectedError();
    }
  }

  Future<SyncEntitlementResponseDto> syncEntitlement(SyncEntitlementRequestDto request) async {
    try {
      final response = await _dio.post(
        '/api/subscriptions/sync',
        data: request.toJson(),
      );
      return SyncEntitlementResponseDto.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      if (e.type == DioExceptionType.connectionTimeout ||
          e.type == DioExceptionType.sendTimeout ||
          e.type == DioExceptionType.receiveTimeout ||
          e.type == DioExceptionType.connectionError) {
        throw const NetworkError();
      }
      throw const UnexpectedError();
    }
  }
}
```

- [ ] **Step 6.5: Update `subscription_repository.dart` interface**

```dart
import '../../data/dtos/start_trial_response_dto.dart';
import '../../data/dtos/subscription_status_response_dto.dart';
import '../../data/dtos/sync_entitlement_request_dto.dart';
import '../../data/dtos/sync_entitlement_response_dto.dart';

abstract class SubscriptionRepository {
  Future<StartTrialResponseDto> startTrial();
  Future<SubscriptionStatusResponseDto> getStatus();
  Future<SyncEntitlementResponseDto> syncEntitlement(SyncEntitlementRequestDto request);
}
```

- [ ] **Step 6.6: Update `subscription_repository_impl.dart`**

```dart
import '../../domain/repositories/subscription_repository.dart';
import '../datasources/subscription_remote_data_source.dart';
import '../dtos/start_trial_response_dto.dart';
import '../dtos/subscription_status_response_dto.dart';
import '../dtos/sync_entitlement_request_dto.dart';
import '../dtos/sync_entitlement_response_dto.dart';

class SubscriptionRepositoryImpl implements SubscriptionRepository {
  const SubscriptionRepositoryImpl(this._dataSource);
  final SubscriptionRemoteDataSource _dataSource;

  @override
  Future<StartTrialResponseDto> startTrial() => _dataSource.startTrial();

  @override
  Future<SubscriptionStatusResponseDto> getStatus() => _dataSource.getStatus();

  @override
  Future<SyncEntitlementResponseDto> syncEntitlement(SyncEntitlementRequestDto request) =>
      _dataSource.syncEntitlement(request);
}
```

- [ ] **Step 6.7: Create `revenue_cat_service.dart`**

```dart
import 'package:purchases_flutter/purchases_flutter.dart';
import '../dtos/sync_entitlement_request_dto.dart';

abstract class RevenueCatService {
  Future<SyncEntitlementRequestDto?> getActiveEntitlement();
}

class RevenueCatServiceImpl implements RevenueCatService {
  static const _entitlementId = 'pro_access';

  @override
  Future<SyncEntitlementRequestDto?> getActiveEntitlement() async {
    final customerInfo = await Purchases.getCustomerInfo();
    final entitlement = customerInfo.entitlements.active[_entitlementId];
    if (entitlement == null) return null;

    final expirationDate = entitlement.expirationDate;
    if (expirationDate == null) return null;

    return SyncEntitlementRequestDto(
      revenueCatCustomerId: customerInfo.originalAppUserId,
      entitlement: _entitlementId,
      plan: _resolvePlan(entitlement.productIdentifier),
      expiresAt: DateTime.parse(expirationDate),
    );
  }

  String _resolvePlan(String productIdentifier) {
    if (productIdentifier.contains('annual') ||
        productIdentifier.contains('yearly') ||
        productIdentifier.contains('anual')) {
      return 'annual';
    }
    return 'monthly';
  }
}
```

---

## Task 7: Flutter State + Controller + Providers + SubscriptionStatusState update

**Files:**
- Create: `apps/mobile/lib/features/subscriptions/presentation/providers/sync_entitlement_state.dart`
- Create: `apps/mobile/lib/features/subscriptions/presentation/providers/sync_entitlement_controller.dart`
- Modify: `apps/mobile/lib/features/subscriptions/presentation/providers/subscription_providers.dart`
- Modify: `apps/mobile/lib/features/subscriptions/presentation/providers/subscription_status_state.dart`

- [ ] **Step 7.1: Create `sync_entitlement_state.dart`**

```dart
import '../../../../core/errors/app_error.dart';

sealed class SyncEntitlementState {
  const SyncEntitlementState();
}

final class SyncEntitlementIdle extends SyncEntitlementState {
  const SyncEntitlementIdle();
}

final class SyncEntitlementLoading extends SyncEntitlementState {
  const SyncEntitlementLoading();
}

final class SyncEntitlementNoActiveSubscription extends SyncEntitlementState {
  const SyncEntitlementNoActiveSubscription();
}

final class SyncEntitlementDone extends SyncEntitlementState {
  const SyncEntitlementDone({
    required this.accessStatus,
    required this.plan,
    required this.expiresAt,
  });

  final String accessStatus;
  final String plan;
  final DateTime expiresAt;

  bool get isActive => accessStatus == 'subscription_active';
}

final class SyncEntitlementError extends SyncEntitlementState {
  const SyncEntitlementError(this.error);
  final AppError error;
}
```

- [ ] **Step 7.2: Create `sync_entitlement_controller.dart`**

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/analytics/analytics_provider.dart';
import '../../../../core/auth/access_status.dart';
import '../../../../core/auth/session_provider.dart';
import '../../../../core/auth/session_state.dart';
import '../../../../core/errors/app_error.dart';
import 'subscription_providers.dart';
import 'sync_entitlement_state.dart';

class SyncEntitlementController extends Notifier<SyncEntitlementState> {
  @override
  SyncEntitlementState build() => const SyncEntitlementIdle();

  Future<void> sync() async {
    state = const SyncEntitlementLoading();

    final analytics = ref.read(analyticsServiceProvider);
    final revenueCatService = ref.read(revenueCatServiceProvider);
    final repository = ref.read(subscriptionRepositoryProvider);

    try {
      final entitlementRequest = await revenueCatService.getActiveEntitlement();

      if (entitlementRequest == null) {
        state = const SyncEntitlementNoActiveSubscription();
        return;
      }

      final response = await repository.syncEntitlement(entitlementRequest);
      final current = ref.read(currentSessionStateProvider);
      final newAccessStatus = parseAccessStatus(response.accessStatus);

      if (newAccessStatus != null && newAccessStatus != current?.accessStatus) {
        if (newAccessStatus == AccessStatus.subscriptionActive) {
          final wasExpired = current?.accessStatus?.isExpired ?? false;
          await analytics.logEvent(wasExpired ? 'access_restored' : 'subscription_started');
        } else if (newAccessStatus == AccessStatus.subscriptionExpired) {
          await analytics.logEvent('subscription_expired');
        }

        ref.read(currentSessionStateProvider.notifier).set(
              SessionState(
                hasSession: true,
                accessStatus: newAccessStatus,
                onboardingCompleted: current?.onboardingCompleted ?? false,
              ),
            );
      }

      state = SyncEntitlementDone(
        accessStatus: response.accessStatus,
        plan: response.plan,
        expiresAt: response.expiresAt,
      );
    } on NetworkError {
      state = const SyncEntitlementError(NetworkError());
    } catch (_) {
      state = const SyncEntitlementError(UnexpectedError());
    }
  }
}

final syncEntitlementControllerProvider =
    NotifierProvider<SyncEntitlementController, SyncEntitlementState>(
        SyncEntitlementController.new);
```

- [ ] **Step 7.3: Update `subscription_providers.dart` to add `revenueCatServiceProvider`**

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/network/dio_client.dart';
import '../../data/datasources/subscription_remote_data_source.dart';
import '../../data/repositories/subscription_repository_impl.dart';
import '../../data/services/revenue_cat_service.dart';
import '../../domain/repositories/subscription_repository.dart';

final subscriptionRemoteDataSourceProvider =
    Provider<SubscriptionRemoteDataSource>((ref) {
  return SubscriptionRemoteDataSource(ref.watch(authenticatedDioProvider));
});

final subscriptionRepositoryProvider = Provider<SubscriptionRepository>((ref) {
  return SubscriptionRepositoryImpl(
      ref.watch(subscriptionRemoteDataSourceProvider));
});

final revenueCatServiceProvider = Provider<RevenueCatService>((ref) {
  return RevenueCatServiceImpl();
});
```

- [ ] **Step 7.4: Update `subscription_status_state.dart` to add paid plan getters**

```dart
import '../../../../core/errors/app_error.dart';

sealed class SubscriptionStatusState {
  const SubscriptionStatusState();
}

final class SubscriptionStatusIdle extends SubscriptionStatusState {
  const SubscriptionStatusIdle();
}

final class SubscriptionStatusLoading extends SubscriptionStatusState {
  const SubscriptionStatusLoading();
}

final class SubscriptionStatusLoaded extends SubscriptionStatusState {
  const SubscriptionStatusLoaded({
    required this.accessStatus,
    this.trialStartedAt,
    this.trialEndsAt,
    this.daysRemaining,
    this.plan,
    this.expiresAt,
  });

  final String accessStatus;
  final DateTime? trialStartedAt;
  final DateTime? trialEndsAt;
  final int? daysRemaining;
  final String? plan;
  final DateTime? expiresAt;

  bool get isTrialActive => accessStatus == 'trial_active';
  bool get isTrialExpired => accessStatus == 'trial_expired';
  bool get isExpiringSoon => isTrialActive && (daysRemaining ?? 99) <= 2;
  bool get isSubscriptionActive => accessStatus == 'subscription_active';
  bool get isSubscriptionExpired => accessStatus == 'subscription_expired';
}

final class SubscriptionStatusError extends SubscriptionStatusState {
  const SubscriptionStatusError(this.error);

  final AppError error;
}
```

- [ ] **Step 7.5: Update `subscription_status_controller.dart` to propagate subscription_active to session**

In the `loadStatus()` method, add handling for `subscription_active` and `subscription_expired` similar to how `trial_expired` is handled. Replace the handler body with:

```dart
Future<void> loadStatus() async {
  state = const SubscriptionStatusLoading();

  final analytics = ref.read(analyticsServiceProvider);
  final repository = ref.read(subscriptionRepositoryProvider);

  try {
    final dto = await repository.getStatus();
    final current = ref.read(currentSessionStateProvider);

    if (dto.accessStatus == 'trial_expired') {
      final wasAlreadyExpired =
          current?.accessStatus == AccessStatus.trialExpired;

      if (!wasAlreadyExpired) {
        await analytics.logEvent('trial_expired');

        ref.read(currentSessionStateProvider.notifier).set(
              SessionState(
                hasSession: true,
                accessStatus: AccessStatus.trialExpired,
                onboardingCompleted: current?.onboardingCompleted ?? false,
              ),
            );

        await analytics.logEvent('access_blocked');
      }
    } else if (dto.accessStatus == 'subscription_expired') {
      final wasAlreadyExpired =
          current?.accessStatus == AccessStatus.subscriptionExpired;

      if (!wasAlreadyExpired) {
        await analytics.logEvent('subscription_expired');

        ref.read(currentSessionStateProvider.notifier).set(
              SessionState(
                hasSession: true,
                accessStatus: AccessStatus.subscriptionExpired,
                onboardingCompleted: current?.onboardingCompleted ?? false,
              ),
            );

        await analytics.logEvent('access_blocked');
      }
    } else if (dto.accessStatus == 'subscription_active') {
      final newStatus = AccessStatus.subscriptionActive;
      if (current?.accessStatus != newStatus) {
        ref.read(currentSessionStateProvider.notifier).set(
              SessionState(
                hasSession: true,
                accessStatus: newStatus,
                onboardingCompleted: current?.onboardingCompleted ?? false,
              ),
            );
      }
    }

    state = SubscriptionStatusLoaded(
      accessStatus: dto.accessStatus,
      trialStartedAt: dto.trialStartedAt,
      trialEndsAt: dto.trialEndsAt,
      daysRemaining: dto.daysRemaining,
      plan: dto.plan,
      expiresAt: dto.expiresAt,
    );
  } on NetworkError {
    state = const SubscriptionStatusError(NetworkError());
  } catch (_) {
    state = const SubscriptionStatusError(UnexpectedError());
  }
}
```

---

## Task 8: Flutter l10n + gen-l10n

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`

- [ ] **Step 8.1: Add sync error keys to `app_pt.arb`** (before the closing `}`)

```json
  "syncEntitlementConnectionError": "Sem conexão. Verifique sua internet e tente novamente.",
  "@syncEntitlementConnectionError": { "description": "Erro de conexão ao sincronizar assinatura" },
  "syncEntitlementUnexpectedError": "Não foi possível sincronizar sua assinatura. Tente novamente.",
  "@syncEntitlementUnexpectedError": { "description": "Erro inesperado ao sincronizar assinatura" }
```

- [ ] **Step 8.2: Add sync error keys to `app_en.arb`** (before the closing `}`)

```json
  "syncEntitlementConnectionError": "No connection. Check your internet and try again.",
  "@syncEntitlementConnectionError": { "description": "Connection error when syncing subscription" },
  "syncEntitlementUnexpectedError": "Could not sync your subscription. Please try again.",
  "@syncEntitlementUnexpectedError": { "description": "Unexpected error when syncing subscription" }
```

- [ ] **Step 8.3: Add sync error keys to `app_es.arb`** (before the closing `}`)

```json
  "syncEntitlementConnectionError": "Sin conexión. Verifica tu internet e intenta de nuevo.",
  "@syncEntitlementConnectionError": { "description": "Error de conexión al sincronizar suscripción" },
  "syncEntitlementUnexpectedError": "No se pudo sincronizar tu suscripción. Por favor intenta de nuevo.",
  "@syncEntitlementUnexpectedError": { "description": "Error inesperado al sincronizar suscripción" }
```

- [ ] **Step 8.4: Regenerate l10n**

Run from `apps/mobile/`:
```
flutter gen-l10n
```
Expected: No errors. `app_localizations.dart` updated.

- [ ] **Step 8.5: Verify Flutter analyzer**

Run from `apps/mobile/`:
```
flutter analyze
```
Expected: No issues

---

## Task 9: Flutter Tests

**Files:**
- Create: `apps/mobile/test/features/subscriptions/presentation/providers/sync_entitlement_controller_test.dart`
- Modify: `apps/mobile/test/features/subscriptions/data/datasources/subscription_remote_data_source_test.dart`
- Modify: `apps/mobile/test/features/subscriptions/presentation/providers/subscription_status_controller_test.dart`

- [ ] **Step 9.1: Create `sync_entitlement_controller_test.dart`**

```dart
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/auth/access_status.dart';
import 'package:awaken/core/auth/session_provider.dart';
import 'package:awaken/core/auth/session_state.dart';
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/subscriptions/data/dtos/sync_entitlement_request_dto.dart';
import 'package:awaken/features/subscriptions/data/dtos/sync_entitlement_response_dto.dart';
import 'package:awaken/features/subscriptions/data/dtos/start_trial_response_dto.dart';
import 'package:awaken/features/subscriptions/data/dtos/subscription_status_response_dto.dart';
import 'package:awaken/features/subscriptions/data/services/revenue_cat_service.dart';
import 'package:awaken/features/subscriptions/domain/repositories/subscription_repository.dart';
import 'package:awaken/features/subscriptions/presentation/providers/subscription_providers.dart';
import 'package:awaken/features/subscriptions/presentation/providers/sync_entitlement_controller.dart';
import 'package:awaken/features/subscriptions/presentation/providers/sync_entitlement_state.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _FixedSessionNotifier extends CurrentSessionState {
  _FixedSessionNotifier(this._initial);
  final SessionState? _initial;
  @override
  SessionState? build() => _initial;
}

class _FakeRevenueCatService implements RevenueCatService {
  _FakeRevenueCatService({required this.result});
  final Future<SyncEntitlementRequestDto?> result;
  @override
  Future<SyncEntitlementRequestDto?> getActiveEntitlement() => result;
}

class _FakeRepository implements SubscriptionRepository {
  _FakeRepository({required this.syncResult, Future<SubscriptionStatusResponseDto>? statusResult})
      : _statusResult = statusResult ??
            Future.value(SubscriptionStatusResponseDto(accessStatus: 'no_trial'));
  final Future<SyncEntitlementResponseDto> syncResult;
  final Future<SubscriptionStatusResponseDto> _statusResult;

  @override
  Future<StartTrialResponseDto> startTrial() => Future.value(
        StartTrialResponseDto(
          accessStatus: 'trial_active',
          trialStartedAt: DateTime.utc(2026, 6, 18),
          trialEndsAt: DateTime.utc(2026, 6, 25),
        ),
      );
  @override
  Future<SubscriptionStatusResponseDto> getStatus() => _statusResult;
  @override
  Future<SyncEntitlementResponseDto> syncEntitlement(SyncEntitlementRequestDto request) =>
      syncResult;
}

class _FakeAnalytics implements AnalyticsService {
  final List<String> events = [];
  @override
  Future<void> logEvent(String name, {Map<String, Object>? params}) async =>
      events.add(name);
}

ProviderContainer _buildContainer({
  required RevenueCatService revenueCat,
  required SubscriptionRepository repository,
  required _FakeAnalytics analytics,
  SessionState? initialSession,
}) {
  return ProviderContainer(overrides: [
    revenueCatServiceProvider.overrideWithValue(revenueCat),
    subscriptionRepositoryProvider.overrideWithValue(repository),
    analyticsServiceProvider.overrideWithValue(analytics),
    if (initialSession != null)
      currentSessionStateProvider
          .overrideWith(() => _FixedSessionNotifier(initialSession)),
  ]);
}

final _expiresAt = DateTime.utc(2026, 7, 18, 10, 0, 0);

void main() {
  group('SyncEntitlementController', () {
    test('initial state is SyncEntitlementIdle', () {
      final analytics = _FakeAnalytics();
      final container = _buildContainer(
        revenueCat: _FakeRevenueCatService(result: Future.value(null)),
        repository: _FakeRepository(
          syncResult: Future.value(SyncEntitlementResponseDto(
            accessStatus: 'subscription_active',
            plan: 'monthly',
            expiresAt: _expiresAt,
          )),
        ),
        analytics: analytics,
      );
      addTearDown(container.dispose);
      expect(container.read(syncEntitlementControllerProvider),
          isA<SyncEntitlementIdle>());
    });

    test('transitions to NoActiveSubscription when RevenueCat returns null',
        () async {
      final analytics = _FakeAnalytics();
      final container = _buildContainer(
        revenueCat: _FakeRevenueCatService(result: Future.value(null)),
        repository: _FakeRepository(
          syncResult: Future.value(SyncEntitlementResponseDto(
            accessStatus: 'subscription_active',
            plan: 'monthly',
            expiresAt: _expiresAt,
          )),
        ),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      await container.read(syncEntitlementControllerProvider.notifier).sync();

      expect(container.read(syncEntitlementControllerProvider),
          isA<SyncEntitlementNoActiveSubscription>());
      expect(analytics.events, isEmpty);
    });

    test('fires subscription_started and updates session on first active sync',
        () async {
      final analytics = _FakeAnalytics();
      final request = SyncEntitlementRequestDto(
        revenueCatCustomerId: 'rc_123',
        entitlement: 'pro_access',
        plan: 'monthly',
        expiresAt: _expiresAt,
      );
      const initialSession = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.trialActive,
        onboardingCompleted: true,
      );
      final container = _buildContainer(
        revenueCat: _FakeRevenueCatService(result: Future.value(request)),
        repository: _FakeRepository(
          syncResult: Future.value(SyncEntitlementResponseDto(
            accessStatus: 'subscription_active',
            plan: 'monthly',
            expiresAt: _expiresAt,
          )),
        ),
        analytics: analytics,
        initialSession: initialSession,
      );
      addTearDown(container.dispose);

      await container.read(syncEntitlementControllerProvider.notifier).sync();

      expect(analytics.events, contains('subscription_started'));
      final session = container.read(currentSessionStateProvider);
      expect(session?.accessStatus, AccessStatus.subscriptionActive);
      final state = container.read(syncEntitlementControllerProvider);
      expect(state, isA<SyncEntitlementDone>());
      expect((state as SyncEntitlementDone).isActive, true);
    });

    test('fires access_restored when restoring from expired subscription',
        () async {
      final analytics = _FakeAnalytics();
      final request = SyncEntitlementRequestDto(
        revenueCatCustomerId: 'rc_123',
        entitlement: 'pro_access',
        plan: 'annual',
        expiresAt: _expiresAt,
      );
      const initialSession = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.subscriptionExpired,
        onboardingCompleted: true,
      );
      final container = _buildContainer(
        revenueCat: _FakeRevenueCatService(result: Future.value(request)),
        repository: _FakeRepository(
          syncResult: Future.value(SyncEntitlementResponseDto(
            accessStatus: 'subscription_active',
            plan: 'annual',
            expiresAt: _expiresAt,
          )),
        ),
        analytics: analytics,
        initialSession: initialSession,
      );
      addTearDown(container.dispose);

      await container.read(syncEntitlementControllerProvider.notifier).sync();

      expect(analytics.events, contains('access_restored'));
    });

    test('fires subscription_expired and updates session when subscription expired',
        () async {
      final analytics = _FakeAnalytics();
      final pastExpiry = DateTime.utc(2026, 5, 1);
      final request = SyncEntitlementRequestDto(
        revenueCatCustomerId: 'rc_123',
        entitlement: 'pro_access',
        plan: 'monthly',
        expiresAt: pastExpiry,
      );
      const initialSession = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.subscriptionActive,
        onboardingCompleted: true,
      );
      final container = _buildContainer(
        revenueCat: _FakeRevenueCatService(result: Future.value(request)),
        repository: _FakeRepository(
          syncResult: Future.value(SyncEntitlementResponseDto(
            accessStatus: 'subscription_expired',
            plan: 'monthly',
            expiresAt: pastExpiry,
          )),
        ),
        analytics: analytics,
        initialSession: initialSession,
      );
      addTearDown(container.dispose);

      await container.read(syncEntitlementControllerProvider.notifier).sync();

      expect(analytics.events, contains('subscription_expired'));
      final session = container.read(currentSessionStateProvider);
      expect(session?.accessStatus, AccessStatus.subscriptionExpired);
    });

    test('transitions to error on NetworkError', () async {
      final analytics = _FakeAnalytics();
      final request = SyncEntitlementRequestDto(
        revenueCatCustomerId: 'rc_123',
        entitlement: 'pro_access',
        plan: 'monthly',
        expiresAt: _expiresAt,
      );
      final container = _buildContainer(
        revenueCat: _FakeRevenueCatService(result: Future.value(request)),
        repository: _FakeRepository(
          syncResult: Future.error(const NetworkError()),
        ),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      await container.read(syncEntitlementControllerProvider.notifier).sync();

      final state = container.read(syncEntitlementControllerProvider);
      expect(state, isA<SyncEntitlementError>());
      expect((state as SyncEntitlementError).error, isA<NetworkError>());
    });

    test('transitions to error on unexpected exception', () async {
      final analytics = _FakeAnalytics();
      final container = _buildContainer(
        revenueCat: _FakeRevenueCatService(
            result: Future.error(Exception('rc_error'))),
        repository: _FakeRepository(
          syncResult: Future.value(SyncEntitlementResponseDto(
            accessStatus: 'subscription_active',
            plan: 'monthly',
            expiresAt: _expiresAt,
          )),
        ),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      await container.read(syncEntitlementControllerProvider.notifier).sync();

      final state = container.read(syncEntitlementControllerProvider);
      expect(state, isA<SyncEntitlementError>());
      expect((state as SyncEntitlementError).error, isA<UnexpectedError>());
    });
  });
}
```

- [ ] **Step 9.2: Add syncEntitlement test group to `subscription_remote_data_source_test.dart`**

Append the following group after the closing `}` of the existing `getStatus` group (before the final `}`):

```dart
  group('SubscriptionRemoteDataSource.syncEntitlement', () {
    test('sends POST to sync endpoint with correct body', () async {
      RequestOptions? captured;
      final adapter = _CapturingAdapter(
        next: _FakeAdapter(
          statusCode: 200,
          body: {
            'accessStatus': 'subscription_active',
            'plan': 'monthly',
            'expiresAt': '2026-07-18T10:00:00.000Z',
          },
        ),
        onCapture: (options) => captured = options,
      );
      final dataSource = SubscriptionRemoteDataSource(_buildDio(adapter));
      final request = SyncEntitlementRequestDto(
        revenueCatCustomerId: 'rc_123',
        entitlement: 'pro_access',
        plan: 'monthly',
        expiresAt: DateTime.utc(2026, 7, 18, 10, 0, 0),
      );

      await dataSource.syncEntitlement(request);

      expect(captured?.method, 'POST');
      expect(captured?.path, '/api/subscriptions/sync');
    });

    test('parses sync response correctly', () async {
      final expiresAt = DateTime.utc(2026, 7, 18, 10, 0, 0);
      final dataSource = SubscriptionRemoteDataSource(
        _buildDio(
          _FakeAdapter(
            statusCode: 200,
            body: {
              'accessStatus': 'subscription_active',
              'plan': 'annual',
              'expiresAt': expiresAt.toIso8601String(),
            },
          ),
        ),
      );
      final request = SyncEntitlementRequestDto(
        revenueCatCustomerId: 'rc_456',
        entitlement: 'pro_access',
        plan: 'annual',
        expiresAt: expiresAt,
      );

      final result = await dataSource.syncEntitlement(request);

      expect(result.accessStatus, 'subscription_active');
      expect(result.plan, 'annual');
      expect(result.expiresAt, expiresAt);
    });

    test('throws NetworkError on connection failure', () async {
      final dataSource = SubscriptionRemoteDataSource(
        _buildDio(_ErrorAdapter(type: DioExceptionType.connectionError)),
      );
      final request = SyncEntitlementRequestDto(
        revenueCatCustomerId: 'rc_789',
        entitlement: 'pro_access',
        plan: 'monthly',
        expiresAt: DateTime.utc(2026, 7, 18),
      );

      expect(
          dataSource.syncEntitlement(request), throwsA(isA<NetworkError>()));
    });
  });
```

Also add the import at the top of the test file:
```dart
import 'package:awaken/features/subscriptions/data/dtos/sync_entitlement_request_dto.dart';
```

- [ ] **Step 9.3: Add subscription_active test to `subscription_status_controller_test.dart`**

Append to the existing `SubscriptionStatusController` group:

```dart
    test('sets subscriptionActive in session when backend returns subscription_active',
        () async {
      final analytics = _FakeAnalytics();
      const initialSession = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.trialExpired,
        onboardingCompleted: true,
      );
      final dto = SubscriptionStatusResponseDto(
        accessStatus: 'subscription_active',
        plan: 'monthly',
        expiresAt: DateTime.utc(2026, 7, 18, 10, 0, 0),
      );
      final container = _buildContainer(
        repository: _FakeRepository(statusResult: Future.value(dto)),
        analytics: analytics,
        initialSession: initialSession,
      );
      addTearDown(container.dispose);

      await container
          .read(subscriptionStatusControllerProvider.notifier)
          .loadStatus();

      final session = container.read(currentSessionStateProvider);
      expect(session?.accessStatus, AccessStatus.subscriptionActive);
    });

    test('transitions to loaded with plan and expiresAt for subscription_active',
        () async {
      final analytics = _FakeAnalytics();
      final expiresAt = DateTime.utc(2026, 7, 18, 10, 0, 0);
      final dto = SubscriptionStatusResponseDto(
        accessStatus: 'subscription_active',
        plan: 'annual',
        expiresAt: expiresAt,
      );
      final container = _buildContainer(
        repository: _FakeRepository(statusResult: Future.value(dto)),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      await container
          .read(subscriptionStatusControllerProvider.notifier)
          .loadStatus();

      final state = container.read(subscriptionStatusControllerProvider);
      expect(state, isA<SubscriptionStatusLoaded>());
      final loaded = state as SubscriptionStatusLoaded;
      expect(loaded.accessStatus, 'subscription_active');
      expect(loaded.isSubscriptionActive, true);
      expect(loaded.plan, 'annual');
      expect(loaded.expiresAt, expiresAt);
    });
```

- [ ] **Step 9.4: Run all Flutter tests**

Run from `apps/mobile/`:
```
flutter test
```
Expected: All tests pass

---

## Task 10: Final Verification

- [ ] **Step 10.1: Run all backend tests**

Run: `dotnet test backend/`
Expected: All tests pass

- [ ] **Step 10.2: Run Flutter analyzer**

Run from `apps/mobile/`: `flutter analyze`
Expected: No issues

- [ ] **Step 10.3: Run Flutter tests**

Run from `apps/mobile/`: `flutter test`
Expected: All tests pass

---

## Self-Review Checklist

### Spec Coverage

| Requirement | Task |
|---|---|
| RN-001: RevenueCat como fonte de status pago | Task 6 (RevenueCatService), Task 7 (SyncEntitlementController) |
| RN-002: Backend mantém cópia do status | Task 1-3 (Subscription entity + migration + handler) |
| RN-003: Assinatura ativa libera recursos | Task 2 (SyncEntitlementCommandHandler returns subscription_active), Task 7 (session update) |
| RN-004: Assinatura expirada bloqueia recursos | Task 2 (returns subscription_expired), Task 7 (session update → blocks page) |
| RN-005: Falha temporária não apaga progresso | Task 7 (SyncEntitlementController: error state, no session mutation on failure) |
| RN-006: Evitar estados contraditórios | Task 2 (GetSubscriptionStatusQueryHandler: paid plan takes priority) |
| CA-001: Assinatura ativa libera | Task 4 (SyncEntitlementCommandHandlerTests.HandleCreatesNewSubscriptionWhenNoneExists) |
| CA-002: Assinatura expirada bloqueia | Task 4 (HandleReturnsExpiredWhenExpiresAtIsInPast) |
| Analytics: subscription_started | Task 7 (SyncEntitlementController fires on first active sync) |
| Analytics: subscription_expired | Task 7 (fires when status becomes subscription_expired) |
| Analytics: access_restored | Task 7 (fires when restoring from expired) |
| PT-BR/EN/ES sync error messages | Task 8 |
| POST /api/subscriptions/sync | Task 3 |
| GET /api/subscriptions/status returns Plan + ExpiresAt | Task 2 (GetSubscriptionStatusQueryHandler updated) |
| EF migration for new columns | Task 3 |
| Trial → paid plan upgrade path | Task 5 (SyncAfterTrialSetsSubscriptionActive integration test) |
