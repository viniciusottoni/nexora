# US-090 — Água em Copos Ajustáveis — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow the user to configure a preferred cup volume (ml), display water consumed as cup count, and use the cup volume in the quick-add button; persist the preference server-side.

**Architecture:** New `UserNutritionPreference` domain entity persists `CupVolumeMl` per user. `GetBasicNutritionToday` response is extended with `CupVolumeMl` (default 250 ml). A new `PATCH /api/nutrition/preferences/cup-volume` endpoint saves the preference. Flutter state, UI widgets, and repository are updated accordingly.

**Tech Stack:** ASP.NET Core 10 (MediatR, FluentValidation, EF Core + PostgreSQL), Flutter 3 (Riverpod, Dio), xUnit + FluentAssertions + Testcontainers.

---

## File Map

### Backend — Create
- `Awaken.Domain/Entities/Nutrition/UserNutritionPreference.cs`
- `Awaken.Domain/Repositories/IUserNutritionPreferenceRepository.cs`
- `Awaken.Infrastructure/Persistence/Configurations/UserNutritionPreferenceConfiguration.cs`
- `Awaken.Infrastructure/Persistence/Repositories/UserNutritionPreferenceRepository.cs`
- `Awaken.Contracts/Nutrition/UpdateCupVolumeRequest.cs`
- `Awaken.Contracts/Nutrition/UpdateCupVolumeResponse.cs`
- `Awaken.Application/Nutrition/Commands/UpdateCupVolume/UpdateCupVolumeCommand.cs`
- `Awaken.Application/Nutrition/Commands/UpdateCupVolume/UpdateCupVolumeCommandValidator.cs`
- `Awaken.Application/Nutrition/Commands/UpdateCupVolume/UpdateCupVolumeCommandHandler.cs`
- `tests/Awaken.UnitTests/Nutrition/UpdateCupVolumeCommandHandlerTests.cs`
- `tests/Awaken.IntegrationTests/NutritionCupVolumeEndpointTests.cs`

### Backend — Modify
- `Awaken.Contracts/Nutrition/BasicNutritionTodayResponse.cs` — add `CupVolumeMl`
- `Awaken.Application/Nutrition/Queries/GetBasicNutritionToday/GetBasicNutritionTodayQueryHandler.cs` — inject preference repo, return `CupVolumeMl`
- `Awaken.Infrastructure/Persistence/AwakenDbContext.cs` — add `UserNutritionPreferences` DbSet
- `Awaken.Infrastructure/DependencyInjection.cs` — register `IUserNutritionPreferenceRepository`
- `Awaken.Api/Controllers/V1/NutritionController.cs` — add PATCH endpoint
- `tests/Awaken.UnitTests/Nutrition/GetBasicNutritionTodayQueryHandlerTests.cs` — add mock + assertions

### Flutter — Create
- `apps/mobile/lib/features/nutrition/data/dtos/update_cup_volume_request_dto.dart`
- `apps/mobile/test/e2e/water_cup_volume_flow_test.dart`

### Flutter — Modify
- `apps/mobile/lib/features/nutrition/domain/entities/daily_water_summary.dart` — add `cupVolumeMl`
- `apps/mobile/lib/features/nutrition/domain/repositories/nutrition_repository.dart` — add `updateCupVolume`
- `apps/mobile/lib/features/nutrition/data/dtos/basic_nutrition_today_response_dto.dart` — add `cupVolumeMl`
- `apps/mobile/lib/features/nutrition/data/datasources/nutrition_remote_data_source.dart` — add `updateCupVolume`
- `apps/mobile/lib/features/nutrition/data/repositories/nutrition_repository_impl.dart` — implement `updateCupVolume`
- `apps/mobile/lib/features/nutrition/presentation/providers/basic_nutrition_state.dart` — add `isSavingCupVolume`
- `apps/mobile/lib/features/nutrition/presentation/providers/basic_nutrition_controller.dart` — add `updateCupVolume`
- `apps/mobile/lib/features/nutrition/presentation/widgets/water_intake_buttons.dart` — cup selector + single add button
- `apps/mobile/lib/features/nutrition/presentation/widgets/water_goal_card.dart` — show cups count, wire up new params
- `apps/mobile/lib/l10n/app_pt.arb` — add `waterCupsCount`
- `apps/mobile/lib/l10n/app_en.arb` — add `waterCupsCount`
- `apps/mobile/lib/l10n/app_es.arb` — add `waterCupsCount`
- `apps/mobile/lib/l10n/app_fr.arb` — add `waterCupsCount`
- `apps/mobile/test/features/nutrition/presentation/providers/basic_nutrition_controller_test.dart`
- `apps/mobile/test/features/nutrition/data/datasources/nutrition_remote_data_source_test.dart`
- `apps/mobile/test/e2e/water_intake_flow_test.dart`

---

## Task 1: Domain Entity `UserNutritionPreference`

**Files:**
- Create: `backend/src/Awaken.Domain/Entities/Nutrition/UserNutritionPreference.cs`

- [ ] **Step 1: Create the entity**

```csharp
using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Nutrition;

public class UserNutritionPreference : BaseEntity
{
    public Guid UserId { get; private set; }
    public int CupVolumeMl { get; private set; }

    private UserNutritionPreference() { }

    public static UserNutritionPreference Create(Guid userId, int cupVolumeMl = 250)
    {
        return new UserNutritionPreference
        {
            UserId = userId,
            CupVolumeMl = cupVolumeMl,
        };
    }

    /// <summary>US-090 RN-001/RN-004: atualiza volume do copo. Validação na camada Application.</summary>
    public void UpdateCupVolume(int cupVolumeMl) => CupVolumeMl = cupVolumeMl;
}
```

---

## Task 2: Repository Interface + EF Config + Implementation + DbContext + DI

**Files:**
- Create: `backend/src/Awaken.Domain/Repositories/IUserNutritionPreferenceRepository.cs`
- Create: `backend/src/Awaken.Infrastructure/Persistence/Configurations/UserNutritionPreferenceConfiguration.cs`
- Create: `backend/src/Awaken.Infrastructure/Persistence/Repositories/UserNutritionPreferenceRepository.cs`
- Modify: `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs`
- Modify: `backend/src/Awaken.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Create repository interface**

```csharp
// Awaken.Domain/Repositories/IUserNutritionPreferenceRepository.cs
using Awaken.Domain.Common;
using Awaken.Domain.Entities.Nutrition;

namespace Awaken.Domain.Repositories;

public interface IUserNutritionPreferenceRepository : IRepository<UserNutritionPreference>
{
    Task<UserNutritionPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Create EF configuration**

```csharp
// Awaken.Infrastructure/Persistence/Configurations/UserNutritionPreferenceConfiguration.cs
using Awaken.Domain.Entities.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class UserNutritionPreferenceConfiguration : IEntityTypeConfiguration<UserNutritionPreference>
{
    public void Configure(EntityTypeBuilder<UserNutritionPreference> builder)
    {
        builder.ToTable("user_nutrition_preferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CupVolumeMl).IsRequired().HasDefaultValue(250);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
```

- [ ] **Step 3: Create repository implementation**

```csharp
// Awaken.Infrastructure/Persistence/Repositories/UserNutritionPreferenceRepository.cs
using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class UserNutritionPreferenceRepository(AwakenDbContext context) : IUserNutritionPreferenceRepository
{
    public async Task<UserNutritionPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.UserNutritionPreferences.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<UserNutritionPreference>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.UserNutritionPreferences.ToListAsync(cancellationToken);

    public async Task AddAsync(UserNutritionPreference entity, CancellationToken cancellationToken = default)
        => await context.UserNutritionPreferences.AddAsync(entity, cancellationToken);

    public void Update(UserNutritionPreference entity)
        => context.UserNutritionPreferences.Update(entity);

    public void Remove(UserNutritionPreference entity)
        => context.UserNutritionPreferences.Remove(entity);

    public async Task<UserNutritionPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserNutritionPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
```

- [ ] **Step 4: Add DbSet to AwakenDbContext**

In `AwakenDbContext.cs`, add after `NutritionLogs`:
```csharp
public DbSet<UserNutritionPreference> UserNutritionPreferences => Set<UserNutritionPreference>();
```

- [ ] **Step 5: Register in DI**

In `DependencyInjection.cs`, add after `INutritionLogRepository`:
```csharp
services.AddScoped<IUserNutritionPreferenceRepository, UserNutritionPreferenceRepository>();
```

---

## Task 3: Contracts

**Files:**
- Create: `backend/src/Awaken.Contracts/Nutrition/UpdateCupVolumeRequest.cs`
- Create: `backend/src/Awaken.Contracts/Nutrition/UpdateCupVolumeResponse.cs`
- Modify: `backend/src/Awaken.Contracts/Nutrition/BasicNutritionTodayResponse.cs`

- [ ] **Step 1: Create UpdateCupVolumeRequest**

```csharp
namespace Awaken.Contracts.Nutrition;

public record UpdateCupVolumeRequest(int CupVolumeMl);
```

- [ ] **Step 2: Create UpdateCupVolumeResponse**

```csharp
namespace Awaken.Contracts.Nutrition;

public record UpdateCupVolumeResponse(int CupVolumeMl);
```

- [ ] **Step 3: Add CupVolumeMl to BasicNutritionTodayResponse**

Replace the record with:
```csharp
namespace Awaken.Contracts.Nutrition;

/// <summary>US-086/US-088/US-090: meta diária de água, consumo, gasto calórico estimado e volume do copo.</summary>
public record BasicNutritionTodayResponse(
    int WaterMinimumMl,
    int WaterIdealMl,
    int WaterConsumedMl,
    int CaloriesSpentEstimatedToday = 0,
    int CaloriesSpentEstimatedUntilNow = 0,
    string CaloriesCalculationStatus = "incomplete",
    int CupVolumeMl = 250);
```

---

## Task 4: Application — UpdateCupVolume Command

**Files:**
- Create: `backend/src/Awaken.Application/Nutrition/Commands/UpdateCupVolume/UpdateCupVolumeCommand.cs`
- Create: `backend/src/Awaken.Application/Nutrition/Commands/UpdateCupVolume/UpdateCupVolumeCommandValidator.cs`
- Create: `backend/src/Awaken.Application/Nutrition/Commands/UpdateCupVolume/UpdateCupVolumeCommandHandler.cs`

- [ ] **Step 1: Create command**

```csharp
using Awaken.Contracts.Nutrition;
using MediatR;

namespace Awaken.Application.Nutrition.Commands.UpdateCupVolume;

public record UpdateCupVolumeCommand(int CupVolumeMl) : IRequest<UpdateCupVolumeResponse>;
```

- [ ] **Step 2: Create validator**

```csharp
using FluentValidation;

namespace Awaken.Application.Nutrition.Commands.UpdateCupVolume;

public class UpdateCupVolumeCommandValidator : AbstractValidator<UpdateCupVolumeCommand>
{
    public UpdateCupVolumeCommandValidator()
    {
        // US-090 RN-005: valores válidos entre 50 e 2000 ml.
        RuleFor(x => x.CupVolumeMl)
            .InclusiveBetween(50, 2000)
            .WithMessage("CupVolumeMl must be between 50 and 2000 ml.");
    }
}
```

- [ ] **Step 3: Create handler**

```csharp
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Nutrition;
using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Nutrition.Commands.UpdateCupVolume;

public class UpdateCupVolumeCommandHandler(
    ICurrentUserService currentUserService,
    IUserNutritionPreferenceRepository preferenceRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateCupVolumeCommand, UpdateCupVolumeResponse>
{
    public async Task<UpdateCupVolumeResponse> Handle(
        UpdateCupVolumeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var preference = await preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        // US-090 RN-004: cria preferência na primeira vez.
        if (preference is null)
        {
            preference = UserNutritionPreference.Create(userId, request.CupVolumeMl);
            await preferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.UpdateCupVolume(request.CupVolumeMl);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateCupVolumeResponse(CupVolumeMl: preference.CupVolumeMl);
    }
}
```

---

## Task 5: Update GetBasicNutritionTodayQueryHandler

**Files:**
- Modify: `backend/src/Awaken.Application/Nutrition/Queries/GetBasicNutritionToday/GetBasicNutritionTodayQueryHandler.cs`

- [ ] **Step 1: Inject `IUserNutritionPreferenceRepository` and return `CupVolumeMl`**

Replace the handler class constructor and `Handle` method (keep all `CalculateCalories` and helper methods unchanged):

```csharp
public class GetBasicNutritionTodayQueryHandler(
    ICurrentUserService currentUserService,
    IUserProfileRepository userProfileRepository,
    INutritionLogRepository nutritionLogRepository,
    IUserDateService userDateService,
    IUserNutritionPreferenceRepository nutritionPreferenceRepository) : IRequestHandler<GetBasicNutritionTodayQuery, BasicNutritionTodayResponse>
{
    private const int MinimumMlPerKg = 30;
    private const int IdealMlPerKg = 50;

    public async Task<BasicNutritionTodayResponse> Handle(
        GetBasicNutritionTodayQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        var today = userDateService.TodayLocal;
        var log = await nutritionLogRepository.GetByUserIdAndDateAsync(userId, today, cancellationToken);
        var waterConsumedMl = log?.WaterMl ?? 0;

        // US-090 RN-004: preferência do volume do copo; padrão 250 ml.
        var preference = await nutritionPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        var cupVolumeMl = preference?.CupVolumeMl ?? 250;

        // US-086 RN-004: sem perfil ou sem peso válido, metas ficam zeradas.
        if (profile is null || profile.WeightKg is null or <= 0)
        {
            return new BasicNutritionTodayResponse(
                WaterMinimumMl: 0,
                WaterIdealMl: 0,
                WaterConsumedMl: waterConsumedMl,
                CupVolumeMl: cupVolumeMl);
        }

        var waterMinimumMl = (int)Math.Round(
            profile.WeightKg.Value * MinimumMlPerKg,
            MidpointRounding.AwayFromZero);
        var waterIdealMl = (int)Math.Round(
            profile.WeightKg.Value * IdealMlPerKg,
            MidpointRounding.AwayFromZero);

        var (caloriesDay, caloriesNow, caloriesStatus) = CalculateCalories(profile, userDateService.NowLocal);

        return new BasicNutritionTodayResponse(
            WaterMinimumMl: waterMinimumMl,
            WaterIdealMl: waterIdealMl,
            WaterConsumedMl: waterConsumedMl,
            CaloriesSpentEstimatedToday: caloriesDay,
            CaloriesSpentEstimatedUntilNow: caloriesNow,
            CaloriesCalculationStatus: caloriesStatus,
            CupVolumeMl: cupVolumeMl);
    }

    // CalculateCalories, InterpretBiologicalSex, ActivityFactor, BodyTypeFactor — UNCHANGED
```

> Note: keep all static helper methods (`CalculateCalories`, `InterpretBiologicalSex`, `ActivityFactor`, `BodyTypeFactor`) exactly as they are. Only add the new repository parameter and update `Handle`.

---

## Task 6: NutritionController — PATCH endpoint

**Files:**
- Modify: `backend/src/Awaken.Api/Controllers/V1/NutritionController.cs`

- [ ] **Step 1: Add using and PATCH action**

Add to the top of the file:
```csharp
using Awaken.Application.Nutrition.Commands.UpdateCupVolume;
```

Add after the `LogWaterIntake` action:
```csharp
/// US-090: persiste o volume preferido do copo.
[HttpPatch("preferences/cup-volume")]
public async Task<IActionResult> UpdateCupVolume(
    [FromBody] UpdateCupVolumeRequest request,
    CancellationToken ct)
{
    var result = await mediator.Send(new UpdateCupVolumeCommand(request.CupVolumeMl), ct);
    return Ok(new
    {
        result.CupVolumeMl,
        correlationId = CorrelationId,
    });
}
```

---

## Task 7: EF Migration

- [ ] **Step 1: Run migration command**

Run from `backend/src/`:
```bash
dotnet ef migrations add AddUserNutritionPreference -p Awaken.Infrastructure -s Awaken.Api
```

Expected: new migration files created in `Awaken.Infrastructure/Persistence/Migrations/`.

- [ ] **Step 2: Verify migration builds**

```bash
dotnet build backend/src/Awaken.Infrastructure/Awaken.Infrastructure.csproj
```

Expected: Build succeeded with 0 errors.

---

## Task 8: Backend Unit Tests

**Files:**
- Create: `backend/tests/Awaken.UnitTests/Nutrition/UpdateCupVolumeCommandHandlerTests.cs`
- Modify: `backend/tests/Awaken.UnitTests/Nutrition/GetBasicNutritionTodayQueryHandlerTests.cs`

- [ ] **Step 1: Write failing unit tests for UpdateCupVolumeCommandHandler**

```csharp
// backend/tests/Awaken.UnitTests/Nutrition/UpdateCupVolumeCommandHandlerTests.cs
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Nutrition.Commands.UpdateCupVolume;
using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Nutrition;

public class UpdateCupVolumeCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserNutritionPreferenceRepository> _preferenceRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateCupVolumeCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private UpdateCupVolumeCommandHandler CreateHandler() => new(
        _currentUserService.Object,
        _preferenceRepository.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task CreatesPreferenceWhenNoneExists()
    {
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNutritionPreference?)null);

        UserNutritionPreference? added = null;
        _preferenceRepository
            .Setup(r => r.AddAsync(It.IsAny<UserNutritionPreference>(), It.IsAny<CancellationToken>()))
            .Callback<UserNutritionPreference, CancellationToken>((e, _) => added = e)
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().Handle(new UpdateCupVolumeCommand(300), CancellationToken.None);

        result.CupVolumeMl.Should().Be(300);
        added.Should().NotBeNull();
        added!.CupVolumeMl.Should().Be(300);
        added.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task UpdatesExistingPreference()
    {
        var existing = UserNutritionPreference.Create(UserId, 250);
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateHandler().Handle(new UpdateCupVolumeCommand(500), CancellationToken.None);

        result.CupVolumeMl.Should().Be(500);
        existing.CupVolumeMl.Should().Be(500);
        _preferenceRepository.Verify(r => r.AddAsync(It.IsAny<UserNutritionPreference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SavesChangesAfterUpdate()
    {
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNutritionPreference?)null);
        _preferenceRepository
            .Setup(r => r.AddAsync(It.IsAny<UserNutritionPreference>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(new UpdateCupVolumeCommand(250), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail (handler not implemented yet — but it was already created in Task 4)**

```bash
dotnet test backend/tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "UpdateCupVolumeCommandHandlerTests" -v n
```

Expected: PASS (handler and domain already implemented in Tasks 1–4).

- [ ] **Step 3: Update GetBasicNutritionTodayQueryHandlerTests to add preference repository mock**

In `backend/tests/Awaken.UnitTests/Nutrition/GetBasicNutritionTodayQueryHandlerTests.cs`, add:

```csharp
// Add this field after _userDateService:
private readonly Mock<IUserNutritionPreferenceRepository> _preferenceRepository = new();
```

In the constructor, add:
```csharp
// Set up preference to return null by default (uses default 250 ml).
_preferenceRepository
    .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
    .ReturnsAsync((Awaken.Domain.Entities.Nutrition.UserNutritionPreference?)null);
```

Update `CreateHandler()`:
```csharp
private GetBasicNutritionTodayQueryHandler CreateHandler() => new(
    _currentUserService.Object,
    _profileRepository.Object,
    _nutritionLogRepository.Object,
    _userDateService.Object,
    _preferenceRepository.Object);
```

Add a new test after the existing ones:
```csharp
[Fact]
public async Task ReturnsPreferenceCupVolumeWhenSet()
{
    var profile = UserProfile.Create(UserId, weightKg: 70m);
    _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(profile);
    _nutritionLogRepository
        .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Awaken.Domain.Entities.Nutrition.NutritionLog?)null);
    var pref = Awaken.Domain.Entities.Nutrition.UserNutritionPreference.Create(UserId, 500);
    _preferenceRepository
        .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(pref);

    var result = await CreateHandler().Handle(new GetBasicNutritionTodayQuery(), CancellationToken.None);

    result.CupVolumeMl.Should().Be(500);
}

[Fact]
public async Task DefaultsCupVolumeToTwoFiftyWhenNoPreference()
{
    var profile = UserProfile.Create(UserId, weightKg: 70m);
    _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(profile);
    _nutritionLogRepository
        .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Awaken.Domain.Entities.Nutrition.NutritionLog?)null);
    // _preferenceRepository already set up to return null in constructor.

    var result = await CreateHandler().Handle(new GetBasicNutritionTodayQuery(), CancellationToken.None);

    result.CupVolumeMl.Should().Be(250);
}
```

- [ ] **Step 4: Run all backend unit tests**

```bash
dotnet test backend/tests/Awaken.UnitTests/Awaken.UnitTests.csproj -v n
```

Expected: all tests PASS.

---

## Task 9: Backend Integration Tests

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/NutritionCupVolumeEndpointTests.cs`

- [ ] **Step 1: Write integration tests**

```csharp
// backend/tests/Awaken.IntegrationTests/NutritionCupVolumeEndpointTests.cs
// US-090: PATCH /api/nutrition/preferences/cup-volume — volume preferido do copo.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Awaken.Contracts.Auth;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class NutritionCupVolumeEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndGetTokenAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Str0ngPass!"
        });
        return (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task StartTrialAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/subscriptions/trial/start", new { });
    }

    [Fact]
    public async Task UpdateCupVolumeReturnsUnauthorizedWithoutToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 300 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCupVolumeReturnsBadRequestForValueBelowMinimum()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_below@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 49 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateCupVolumeReturnsBadRequestForValueAboveMaximum()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_above@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 2001 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateCupVolumePersistsPreference()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_persist@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patchResponse = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 500 });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await patchResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task UpdateCupVolumeReflectsInGetBasicNutritionToday()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_reflect@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 350 });

        var getResponse = await _client.GetAsync("/api/nutrition/basic/today");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(350);
    }

    [Fact]
    public async Task GetBasicNutritionTodayDefaultsCupVolumeToTwoFifty()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_default@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var getResponse = await _client.GetAsync("/api/nutrition/basic/today");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(250);
    }

    [Fact]
    public async Task UpdateCupVolumeOverwritesPreviousPreference()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_overwrite@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 200 });
        var second = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 400 });

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task UpdateCupVolumeIncludesCorrelationId()
    {
        var token = await RegisterAndGetTokenAsync("nutrition090_corr@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 300 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CA001_RecalculatesCupsWhenVolumeChanges()
    {
        // US-090 CA-001: 1000 ml consumido / 250 ml copo = 4 copos → / 500 ml copo = 2 copos.
        // This verifies the endpoint persists the new volume; cup recalculation is client-side.
        var token = await RegisterAndGetTokenAsync("nutrition090_ca001@awaken.app");
        await StartTrialAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Log 1000 ml of water.
        await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 500 });
        await _client.PostAsJsonAsync("/api/nutrition/water", new { amountMl = 500 });

        // Change cup to 500 ml.
        await _client.PatchAsJsonAsync("/api/nutrition/preferences/cup-volume", new { cupVolumeMl = 500 });

        var response = await _client.GetAsync("/api/nutrition/basic/today");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("waterConsumedMl").GetInt32().Should().Be(1000);
        doc.RootElement.GetProperty("cupVolumeMl").GetInt32().Should().Be(500);
        // Client-side: 1000 / 500 = 2 cups (verified in Flutter tests).
    }
}
```

- [ ] **Step 2: Run integration tests**

```bash
dotnet test backend/tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj --filter "NutritionCupVolumeEndpointTests" -v n
```

Expected: all 8 tests PASS.

- [ ] **Step 3: Run all backend tests to check for regressions**

```bash
dotnet test backend/tests/ -v n
```

Expected: all tests PASS.

---

## Task 10: Flutter — ARB + l10n

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`
- Modify: `apps/mobile/lib/l10n/app_fr.arb`

- [ ] **Step 1: Add `waterCupsCount` to each ARB file**

In `app_pt.arb`, after the `waterCupVolumeInvalidMessage` block:
```json
  "waterCupsCount": "{count} copos",
  "@waterCupsCount": {
    "description": "US-090: quantidade de copos consumidos",
    "placeholders": { "count": { "type": "String" } }
  }
```

In `app_en.arb`, after `waterCupVolumeInvalidMessage`:
```json
  "waterCupsCount": "{count} cups",
  "@waterCupsCount": {
    "description": "US-090: number of cups consumed",
    "placeholders": { "count": { "type": "String" } }
  }
```

In `app_es.arb`, after `waterCupVolumeInvalidMessage`:
```json
  "waterCupsCount": "{count} vasos",
  "@waterCupsCount": {
    "description": "US-090: cantidad de vasos consumidos",
    "placeholders": { "count": { "type": "String" } }
  }
```

In `app_fr.arb`, after `waterCupVolumeInvalidMessage`:
```json
  "waterCupsCount": "{count} verres",
  "@waterCupsCount": {
    "description": "US-090: nombre de verres consommés",
    "placeholders": { "count": { "type": "String" } }
  }
```

- [ ] **Step 2: Regenerate l10n**

```bash
cd apps/mobile && flutter gen-l10n
```

Expected: `app_localizations.dart` and the 4 `app_localizations_*.dart` files updated with `waterCupsCount(String count)`.

---

## Task 11: Flutter Domain Layer

**Files:**
- Modify: `apps/mobile/lib/features/nutrition/domain/entities/daily_water_summary.dart`
- Modify: `apps/mobile/lib/features/nutrition/domain/repositories/nutrition_repository.dart`

- [ ] **Step 1: Add `cupVolumeMl` to DailyWaterSummary**

Replace the file entirely:

```dart
/// US-086/US-087/US-088/US-090: resumo de nutrição básica do dia (hidratação + calorias estimadas + volume do copo).
class DailyWaterSummary {
  const DailyWaterSummary({
    required this.waterMinimumMl,
    required this.waterIdealMl,
    required this.waterConsumedMl,
    this.caloriesSpentEstimatedToday = 0,
    this.caloriesSpentEstimatedUntilNow = 0,
    this.caloriesCalculationStatus = 'incomplete',
    this.cupVolumeMl = 250,
  });

  final int waterMinimumMl;
  final int waterIdealMl;
  final int waterConsumedMl;

  /// US-088: gasto calórico estimado para o dia inteiro (kcal).
  final int caloriesSpentEstimatedToday;

  /// US-088: gasto calórico estimado acumulado desde meia-noite até agora (kcal).
  final int caloriesSpentEstimatedUntilNow;

  /// US-088: "estimated" quando cálculo disponível, "incomplete" quando dados do perfil insuficientes.
  final String caloriesCalculationStatus;

  /// US-090 RN-004: volume do copo preferido em ml; padrão 250 ml.
  final int cupVolumeMl;

  /// US-086 RN-004: meta ausente (peso não configurado) quando mínimo == 0.
  bool get hasGoal => waterMinimumMl > 0;

  /// US-088: true quando dados físicos suficientes para calcular calorias.
  bool get hasCaloriesEstimate => caloriesCalculationStatus == 'estimated';

  double get progressToMinimum =>
      waterMinimumMl > 0 ? (waterConsumedMl / waterMinimumMl).clamp(0.0, 1.0) : 0;

  double get progressToIdeal =>
      waterIdealMl > 0 ? (waterConsumedMl / waterIdealMl).clamp(0.0, 1.0) : 0;

  /// US-090 RN-002: quantidade de copos consumidos com base no volume atual.
  double get cupsConsumed => cupVolumeMl > 0 ? waterConsumedMl / cupVolumeMl : 0;

  /// US-090 RN-002: formata quantidade de copos para exibição (inteiro ou 1 decimal).
  String get formattedCupsConsumed {
    final cups = cupsConsumed;
    if (cups == cups.floorToDouble()) return cups.toInt().toString();
    return cups.toStringAsFixed(1);
  }

  DailyWaterSummary copyWith({int? waterConsumedMl, int? cupVolumeMl}) {
    return DailyWaterSummary(
      waterMinimumMl: waterMinimumMl,
      waterIdealMl: waterIdealMl,
      waterConsumedMl: waterConsumedMl ?? this.waterConsumedMl,
      caloriesSpentEstimatedToday: caloriesSpentEstimatedToday,
      caloriesSpentEstimatedUntilNow: caloriesSpentEstimatedUntilNow,
      caloriesCalculationStatus: caloriesCalculationStatus,
      cupVolumeMl: cupVolumeMl ?? this.cupVolumeMl,
    );
  }
}
```

- [ ] **Step 2: Add `updateCupVolume` to NutritionRepository**

Replace the file:
```dart
import '../entities/daily_water_summary.dart';

abstract interface class NutritionRepository {
  /// US-086: retorna meta diária de água e consumo atual do dia.
  Future<DailyWaterSummary> getBasicNutritionToday();

  /// US-087: registra consumo de água e retorna total acumulado no dia.
  Future<DailyWaterSummary> logWaterIntake(int amountMl);

  /// US-090 RN-001/RN-004: persiste volume preferido do copo e retorna resumo atualizado.
  Future<DailyWaterSummary> updateCupVolume(int cupVolumeMl);
}
```

---

## Task 12: Flutter Data Layer

**Files:**
- Modify: `apps/mobile/lib/features/nutrition/data/dtos/basic_nutrition_today_response_dto.dart`
- Create: `apps/mobile/lib/features/nutrition/data/dtos/update_cup_volume_request_dto.dart`
- Modify: `apps/mobile/lib/features/nutrition/data/datasources/nutrition_remote_data_source.dart`
- Modify: `apps/mobile/lib/features/nutrition/data/repositories/nutrition_repository_impl.dart`

- [ ] **Step 1: Add `cupVolumeMl` to BasicNutritionTodayResponseDto**

Replace the file:
```dart
import '../../domain/entities/daily_water_summary.dart';

class BasicNutritionTodayResponseDto {
  const BasicNutritionTodayResponseDto({
    required this.waterMinimumMl,
    required this.waterIdealMl,
    required this.waterConsumedMl,
    required this.caloriesSpentEstimatedToday,
    required this.caloriesSpentEstimatedUntilNow,
    required this.caloriesCalculationStatus,
    required this.cupVolumeMl,
  });

  final int waterMinimumMl;
  final int waterIdealMl;
  final int waterConsumedMl;
  final int caloriesSpentEstimatedToday;
  final int caloriesSpentEstimatedUntilNow;
  final String caloriesCalculationStatus;
  final int cupVolumeMl;

  factory BasicNutritionTodayResponseDto.fromJson(Map<String, dynamic> json) {
    return BasicNutritionTodayResponseDto(
      waterMinimumMl: json['waterMinimumMl'] as int? ?? 0,
      waterIdealMl: json['waterIdealMl'] as int? ?? 0,
      waterConsumedMl: json['waterConsumedMl'] as int? ?? 0,
      caloriesSpentEstimatedToday:
          json['caloriesSpentEstimatedToday'] as int? ?? 0,
      caloriesSpentEstimatedUntilNow:
          json['caloriesSpentEstimatedUntilNow'] as int? ?? 0,
      caloriesCalculationStatus:
          json['caloriesCalculationStatus'] as String? ?? 'incomplete',
      cupVolumeMl: json['cupVolumeMl'] as int? ?? 250,
    );
  }

  DailyWaterSummary toEntity() => DailyWaterSummary(
        waterMinimumMl: waterMinimumMl,
        waterIdealMl: waterIdealMl,
        waterConsumedMl: waterConsumedMl,
        caloriesSpentEstimatedToday: caloriesSpentEstimatedToday,
        caloriesSpentEstimatedUntilNow: caloriesSpentEstimatedUntilNow,
        caloriesCalculationStatus: caloriesCalculationStatus,
        cupVolumeMl: cupVolumeMl,
      );
}
```

- [ ] **Step 2: Create UpdateCupVolumeRequestDto**

```dart
// apps/mobile/lib/features/nutrition/data/dtos/update_cup_volume_request_dto.dart
class UpdateCupVolumeRequestDto {
  const UpdateCupVolumeRequestDto({required this.cupVolumeMl});
  final int cupVolumeMl;
  Map<String, dynamic> toJson() => {'cupVolumeMl': cupVolumeMl};
}
```

- [ ] **Step 3: Add `updateCupVolume` to NutritionRemoteDataSource**

Add import at top:
```dart
import '../dtos/update_cup_volume_request_dto.dart';
```

Add method after `logWaterIntake`:
```dart
/// US-090: persiste volume preferido do copo.
Future<void> updateCupVolume(int cupVolumeMl) async {
  try {
    await _dio.patch(
      '/api/nutrition/preferences/cup-volume',
      data: UpdateCupVolumeRequestDto(cupVolumeMl: cupVolumeMl).toJson(),
    );
  } on DioException catch (e) {
    throw _mapError(e);
  }
}
```

- [ ] **Step 4: Implement `updateCupVolume` in NutritionRepositoryImpl**

Replace the file:
```dart
import '../../domain/entities/daily_water_summary.dart';
import '../../domain/repositories/nutrition_repository.dart';
import '../datasources/nutrition_remote_data_source.dart';

class NutritionRepositoryImpl implements NutritionRepository {
  const NutritionRepositoryImpl(this._dataSource);
  final NutritionRemoteDataSource _dataSource;

  @override
  Future<DailyWaterSummary> getBasicNutritionToday() async {
    final dto = await _dataSource.getBasicNutritionToday();
    return dto.toEntity();
  }

  @override
  Future<DailyWaterSummary> logWaterIntake(int amountMl) async {
    final logResult = await _dataSource.logWaterIntake(amountMl);
    final today = await _dataSource.getBasicNutritionToday();
    return today.toEntity().copyWith(waterConsumedMl: logResult.waterConsumedMl);
  }

  @override
  Future<DailyWaterSummary> updateCupVolume(int cupVolumeMl) async {
    await _dataSource.updateCupVolume(cupVolumeMl);
    final today = await _dataSource.getBasicNutritionToday();
    return today.toEntity();
  }
}
```

---

## Task 13: Flutter Presentation — State + Controller

**Files:**
- Modify: `apps/mobile/lib/features/nutrition/presentation/providers/basic_nutrition_state.dart`
- Modify: `apps/mobile/lib/features/nutrition/presentation/providers/basic_nutrition_controller.dart`

- [ ] **Step 1: Add `isSavingCupVolume` to BasicNutritionLoaded**

Replace `BasicNutritionLoaded` class:
```dart
/// US-086/US-087/US-090: dados carregados com sucesso.
final class BasicNutritionLoaded extends BasicNutritionState {
  const BasicNutritionLoaded(
    this.summary, {
    this.isLoggingWater = false,
    this.isSavingCupVolume = false,
  });
  final DailyWaterSummary summary;
  final bool isLoggingWater;
  final bool isSavingCupVolume;
}
```

- [ ] **Step 2: Add `updateCupVolume` to BasicNutritionController**

After the `logWater` method, add:
```dart
/// US-090 fluxo principal: persiste novo volume do copo e atualiza estado.
Future<void> updateCupVolume(int cupVolumeMl) async {
  final current = state;
  if (current is! BasicNutritionLoaded) return;

  state = BasicNutritionLoaded(current.summary, isSavingCupVolume: true);

  try {
    final updated = await _repository.updateCupVolume(cupVolumeMl);
    state = BasicNutritionLoaded(updated);
    await ref.read(analyticsServiceProvider).logEvent('water_cup_volume_changed');
  } on AccessBlockedError {
    state = const BasicNutritionAccessBlocked();
  } on NetworkError {
    // US-090 RN-005: erro de rede reverte ao estado anterior.
    state = BasicNutritionLoaded(current.summary);
  } catch (_) {
    state = BasicNutritionLoaded(current.summary);
  }
}
```

---

## Task 14: Flutter UI — WaterIntakeButtons

**Files:**
- Modify: `apps/mobile/lib/features/nutrition/presentation/widgets/water_intake_buttons.dart`

- [ ] **Step 1: Refactor WaterIntakeButtons to cup selector + add button**

Replace the entire file:
```dart
import 'package:flutter/material.dart';
import '../../../../design_system/tokens/colors.dart';
import '../../../../design_system/tokens/spacing.dart';
import '../../../../design_system/tokens/typography.dart';
import '../../../../l10n/app_localizations.dart';

/// US-090: seletor de volume do copo + botão de adicionar 1 copo.
class WaterIntakeButtons extends StatelessWidget {
  const WaterIntakeButtons({
    super.key,
    required this.cupVolumeMl,
    required this.onCupVolumeChanged,
    required this.onAddCup,
    this.isLoading = false,
    this.isSavingVolume = false,
  });

  final int cupVolumeMl;
  final void Function(int ml) onCupVolumeChanged;
  final void Function(int ml) onAddCup;
  final bool isLoading;
  final bool isSavingVolume;

  static const _presets = [150, 200, 250, 300, 350, 500];

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Cup volume label
        Row(
          children: [
            Text(
              l10n.waterCupVolumeTitle.toUpperCase(),
              style: AwakenTypography.labelSmall.copyWith(
                color: AwakenColors.textMuted,
                letterSpacing: 1.2,
              ),
            ),
            if (isSavingVolume) ...[
              const SizedBox(width: AwakenSpacing.xs),
              const SizedBox.square(
                dimension: 10,
                child: CircularProgressIndicator(
                  strokeWidth: 1.5,
                  color: AwakenColors.primary,
                ),
              ),
            ],
          ],
        ),
        const SizedBox(height: AwakenSpacing.xs),

        // Preset chips
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: _presets.map((ml) {
              final selected = ml == cupVolumeMl;
              return Padding(
                padding: const EdgeInsets.only(right: AwakenSpacing.xs),
                child: _CupChip(
                  label: '$ml ml',
                  selected: selected,
                  onTap: isSavingVolume ? null : () => onCupVolumeChanged(ml),
                ),
              );
            }).toList(),
          ),
        ),
        const SizedBox(height: AwakenSpacing.sm),

        // Add 1 cup button
        SizedBox(
          width: double.infinity,
          child: _AddCupButton(
            label: l10n.waterAddCupButton(cupVolumeMl),
            onTap: (isLoading || isSavingVolume) ? null : () => onAddCup(cupVolumeMl),
            isLoading: isLoading,
          ),
        ),
      ],
    );
  }
}

class _CupChip extends StatelessWidget {
  const _CupChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(
          horizontal: AwakenSpacing.sm,
          vertical: AwakenSpacing.xxs,
        ),
        decoration: BoxDecoration(
          color: selected
              ? AwakenColors.primary.withValues(alpha: 0.18)
              : AwakenColors.primary.withValues(alpha: 0.05),
          borderRadius: BorderRadius.circular(AwakenSpacing.buttonRadius),
          border: Border.all(
            color: selected
                ? AwakenColors.primary.withValues(alpha: 0.7)
                : AwakenColors.primary.withValues(alpha: 0.15),
          ),
        ),
        child: Text(
          label,
          style: AwakenTypography.stat.copyWith(
            color: selected ? AwakenColors.primary : AwakenColors.textMuted,
            fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
            fontSize: 11,
          ),
        ),
      ),
    );
  }
}

class _AddCupButton extends StatefulWidget {
  const _AddCupButton({
    required this.label,
    required this.onTap,
    this.isLoading = false,
  });

  final String label;
  final VoidCallback? onTap;
  final bool isLoading;

  @override
  State<_AddCupButton> createState() => _AddCupButtonState();
}

class _AddCupButtonState extends State<_AddCupButton> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    final enabled = widget.onTap != null;
    return GestureDetector(
      onTapDown: enabled ? (_) => setState(() => _pressed = true) : null,
      onTapUp: enabled ? (_) => setState(() => _pressed = false) : null,
      onTapCancel: enabled ? () => setState(() => _pressed = false) : null,
      onTap: widget.onTap,
      child: AnimatedScale(
        scale: _pressed ? 0.97 : 1.0,
        duration: const Duration(milliseconds: 100),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 120),
          height: AwakenSpacing.controlSm,
          decoration: BoxDecoration(
            color: AwakenColors.primary.withValues(alpha: enabled ? 0.12 : 0.05),
            borderRadius: BorderRadius.circular(AwakenSpacing.buttonRadius),
            border: Border.all(
              color: AwakenColors.primary.withValues(alpha: enabled ? 0.35 : 0.1),
            ),
          ),
          alignment: Alignment.center,
          child: widget.isLoading
              ? const SizedBox.square(
                  dimension: 14,
                  child: CircularProgressIndicator(
                    strokeWidth: 1.5,
                    color: AwakenColors.primary,
                  ),
                )
              : Text(
                  widget.label,
                  style: AwakenTypography.stat.copyWith(
                    color: enabled ? AwakenColors.primary : AwakenColors.textDisabled,
                    fontWeight: FontWeight.w700,
                    fontSize: 12,
                  ),
                  textAlign: TextAlign.center,
                ),
        ),
      ),
    );
  }
}
```

---

## Task 15: Flutter UI — WaterGoalCard

**Files:**
- Modify: `apps/mobile/lib/features/nutrition/presentation/widgets/water_goal_card.dart`

- [ ] **Step 1: Update `_WaterCardLoaded` to show cups count and wire new button params**

In `_WaterCardLoaded`:
1. Add `isSavingCupVolume` to constructor.
2. Add cups count display below the consumed big number.
3. Update `WaterIntakeButtons` call.

Replace `_WaterCardLoaded` class:
```dart
class _WaterCardLoaded extends StatelessWidget {
  const _WaterCardLoaded({
    required this.summary,
    required this.isLoggingWater,
    required this.isSavingCupVolume,
    required this.l10n,
    required this.onAddCup,
    required this.onCupVolumeChanged,
  });

  final DailyWaterSummary summary;
  final bool isLoggingWater;
  final bool isSavingCupVolume;
  final AppLocalizations l10n;
  final void Function(int ml) onAddCup;
  final void Function(int ml) onCupVolumeChanged;

  String _formatMl(int ml) {
    if (ml >= 1000) {
      final liters = (ml / 1000).toStringAsFixed(1);
      return l10n.waterAmountL(liters);
    }
    return l10n.waterAmountMl(ml);
  }

  @override
  Widget build(BuildContext context) {
    return AwakenPanel(
      padding: const EdgeInsets.all(AwakenSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header
          Row(
            children: [
              const Icon(Icons.water_drop_outlined,
                  color: AwakenColors.primary, size: 18),
              const SizedBox(width: AwakenSpacing.xs),
              Text(
                l10n.waterGoalCardTitle.toUpperCase(),
                style: AwakenTypography.labelSmall.copyWith(
                  color: AwakenColors.primary,
                  letterSpacing: 1.4,
                ),
              ),
            ],
          ),
          const SizedBox(height: AwakenSpacing.smd),

          // Consumed big number + cups count
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                _formatMl(summary.waterConsumedMl),
                style: AwakenTypography.displayMedium.copyWith(
                  color: AwakenColors.primary,
                  fontFamily: AwakenTypography.monoFamily,
                ),
              ),
              const SizedBox(width: AwakenSpacing.sm),
              Padding(
                padding: const EdgeInsets.only(bottom: 4),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      l10n.waterConsumedLabel,
                      style: AwakenTypography.bodySmall,
                    ),
                    Text(
                      l10n.waterCupsCount(summary.formattedCupsConsumed),
                      style: AwakenTypography.labelSmall.copyWith(
                        color: AwakenColors.textMuted,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: AwakenSpacing.sm),

          // Progress bar toward ideal
          AwakenProgressTrack(
            progress: summary.progressToIdeal,
            color: AwakenColors.primary,
            height: 10,
          ),
          const SizedBox(height: AwakenSpacing.xs),

          // Min / ideal labels
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              _GoalLabel(
                label: l10n.waterMinimumLabel,
                value: _formatMl(summary.waterMinimumMl),
                reached: summary.waterConsumedMl >= summary.waterMinimumMl,
              ),
              _GoalLabel(
                label: l10n.waterIdealLabel,
                value: _formatMl(summary.waterIdealMl),
                reached: summary.waterConsumedMl >= summary.waterIdealMl,
                alignRight: true,
              ),
            ],
          ),
          const SizedBox(height: AwakenSpacing.md),

          // Cup volume selector + add button
          WaterIntakeButtons(
            cupVolumeMl: summary.cupVolumeMl,
            onCupVolumeChanged: onCupVolumeChanged,
            onAddCup: onAddCup,
            isLoading: isLoggingWater,
            isSavingVolume: isSavingCupVolume,
          ),
          const SizedBox(height: AwakenSpacing.sm),

          // Basic disclaimer
          Text(
            l10n.waterGoalBasicDisclaimer,
            style: AwakenTypography.bodySmall.copyWith(
              color: AwakenColors.textDisabled,
              fontSize: 10,
            ),
          ),
        ],
      ),
    );
  }
}
```

Also update the `WaterGoalCard.build` switch case for `BasicNutritionLoaded`:
```dart
BasicNutritionLoaded(:final summary, :final isLoggingWater, :final isSavingCupVolume) =>
  _WaterCardLoaded(
    summary: summary,
    isLoggingWater: isLoggingWater,
    isSavingCupVolume: isSavingCupVolume,
    l10n: l10n,
    onAddCup: (ml) =>
        ref.read(basicNutritionControllerProvider.notifier).logWater(ml),
    onCupVolumeChanged: (ml) =>
        ref.read(basicNutritionControllerProvider.notifier).updateCupVolume(ml),
  ),
```

---

## Task 16: Flutter Unit Tests

**Files:**
- Modify: `apps/mobile/test/features/nutrition/presentation/providers/basic_nutrition_controller_test.dart`
- Modify: `apps/mobile/test/features/nutrition/data/datasources/nutrition_remote_data_source_test.dart`

- [ ] **Step 1: Update `_FakeRepository` and add `updateCupVolume` tests**

In `basic_nutrition_controller_test.dart`:

Update the `_FakeRepository` class to add `updateCupVolume`:
```dart
class _FakeRepository implements NutritionRepository {
  _FakeRepository({
    this.todayResult,
    this.todayError,
    this.logResult,
    this.logError,
    this.updateCupVolumeResult,
    this.updateCupVolumeError,
  });

  final DailyWaterSummary? todayResult;
  final Object? todayError;
  final DailyWaterSummary? logResult;
  final Object? logError;
  final DailyWaterSummary? updateCupVolumeResult;
  final Object? updateCupVolumeError;

  @override
  Future<DailyWaterSummary> getBasicNutritionToday() async {
    if (todayError != null) throw todayError!;
    return todayResult!;
  }

  @override
  Future<DailyWaterSummary> logWaterIntake(int amountMl) async {
    if (logError != null) throw logError!;
    return logResult ?? todayResult!;
  }

  @override
  Future<DailyWaterSummary> updateCupVolume(int cupVolumeMl) async {
    if (updateCupVolumeError != null) throw updateCupVolumeError!;
    return updateCupVolumeResult ?? todayResult!.copyWith(cupVolumeMl: cupVolumeMl);
  }
}
```

Add test group after the existing `US-088 caloric fields` group:
```dart
group('BasicNutritionController — US-090 cup volume', () {
  test('updateCupVolume does nothing when state is not BasicNutritionLoaded', () async {
    final container =
        _buildContainer(_FakeRepository(todayResult: _goalSummary));
    addTearDown(container.dispose);

    await container
        .read(basicNutritionControllerProvider.notifier)
        .updateCupVolume(500);

    expect(container.read(basicNutritionControllerProvider),
        isA<BasicNutritionLoading>());
  });

  test('updateCupVolume transitions to isSavingCupVolume then updates state', () async {
    final container = _buildContainer(_FakeRepository(
      todayResult: _goalSummary,
      updateCupVolumeResult: DailyWaterSummary(
        waterMinimumMl: 2100,
        waterIdealMl: 3500,
        waterConsumedMl: 0,
        cupVolumeMl: 500,
      ),
    ));
    addTearDown(container.dispose);

    await container
        .read(basicNutritionControllerProvider.notifier)
        .load();
    await container
        .read(basicNutritionControllerProvider.notifier)
        .updateCupVolume(500);

    final state = container.read(basicNutritionControllerProvider);
    expect(state, isA<BasicNutritionLoaded>());
    expect((state as BasicNutritionLoaded).summary.cupVolumeMl, 500);
  });

  test('updateCupVolume reverts to previous state on network error', () async {
    final container = _buildContainer(_FakeRepository(
      todayResult: _goalSummary,
      updateCupVolumeError: const NetworkError(),
    ));
    addTearDown(container.dispose);

    await container
        .read(basicNutritionControllerProvider.notifier)
        .load();
    await container
        .read(basicNutritionControllerProvider.notifier)
        .updateCupVolume(500);

    final state = container.read(basicNutritionControllerProvider);
    expect(state, isA<BasicNutritionLoaded>());
    expect((state as BasicNutritionLoaded).summary.cupVolumeMl, 250);
  });

  test('updateCupVolume transitions to AccessBlocked on AccessBlockedError', () async {
    final container = _buildContainer(_FakeRepository(
      todayResult: _goalSummary,
      updateCupVolumeError: const AccessBlockedError(),
    ));
    addTearDown(container.dispose);

    await container
        .read(basicNutritionControllerProvider.notifier)
        .load();
    await container
        .read(basicNutritionControllerProvider.notifier)
        .updateCupVolume(500);

    expect(container.read(basicNutritionControllerProvider),
        isA<BasicNutritionAccessBlocked>());
  });

  test('logs water_cup_volume_changed after successful update', () async {
    final analytics = _RecordingAnalyticsService();
    final container = ProviderContainer(overrides: [
      nutritionRepositoryProvider.overrideWithValue(
        _FakeRepository(
          todayResult: _goalSummary,
          updateCupVolumeResult: DailyWaterSummary(
            waterMinimumMl: 2100,
            waterIdealMl: 3500,
            waterConsumedMl: 0,
            cupVolumeMl: 300,
          ),
        ),
      ),
      analyticsServiceProvider.overrideWithValue(analytics),
    ]);
    addTearDown(container.dispose);

    await container.read(basicNutritionControllerProvider.notifier).load();
    await container.read(basicNutritionControllerProvider.notifier).updateCupVolume(300);

    expect(analytics.events, contains('water_cup_volume_changed'));
  });
});
```

Also update the `_goalSummary` constant to include `cupVolumeMl: 250` (or leave as implicit default since the field defaults to 250 — no change needed, but make explicit for clarity):

The existing `_goalSummary` uses `const DailyWaterSummary(...)` without `cupVolumeMl`, which will default to 250 — this is fine.

- [ ] **Step 2: Add datasource tests for `updateCupVolume`**

In `nutrition_remote_data_source_test.dart`, add after the `logWaterIntake` group:
```dart
group('NutritionRemoteDataSource.updateCupVolume', () {
  test('completes successfully on 200', () async {
    final ds = _build(_FakeAdapter(
      statusCode: 200,
      body: {'cupVolumeMl': 300, 'correlationId': 'abc'},
    ));

    await expectLater(ds.updateCupVolume(300), completes);
  });

  test('throws AccessBlockedError on 403', () async {
    final ds = _build(_ErrorAdapter(
        type: DioExceptionType.badResponse, statusCode: 403));

    expect(ds.updateCupVolume(300), throwsA(isA<AccessBlockedError>()));
  });

  test('throws NetworkError on connection error', () async {
    final ds = _build(_ErrorAdapter(type: DioExceptionType.connectionError));

    expect(ds.updateCupVolume(300), throwsA(isA<NetworkError>()));
  });

  test('parses cupVolumeMl from basic/today after update reflects new value', () async {
    final ds = _build(_FakeAdapter(
      statusCode: 200,
      body: {
        'waterMinimumMl': 2100,
        'waterIdealMl': 3500,
        'waterConsumedMl': 0,
        'cupVolumeMl': 350,
        'correlationId': 'abc',
      },
    ));

    final result = await ds.getBasicNutritionToday();

    expect(result.cupVolumeMl, 350);
  });
});
```

- [ ] **Step 3: Run unit tests**

```bash
cd apps/mobile && flutter test test/features/nutrition/ -v
```

Expected: all tests PASS.

---

## Task 17: Flutter E2E Tests

**Files:**
- Modify: `apps/mobile/test/e2e/water_intake_flow_test.dart`
- Create: `apps/mobile/test/e2e/water_cup_volume_flow_test.dart`

- [ ] **Step 1: Update `_FakeNutritionRepository` in water_intake_flow_test.dart**

Add `updateCupVolume` to the fake repo and update `CA-001 US-087` test to use the add-cup button:

Add to `_FakeNutritionRepository`:
```dart
@override
Future<DailyWaterSummary> updateCupVolume(int cupVolumeMl) async {
  _current = _current.copyWith(cupVolumeMl: cupVolumeMl);
  return _current;
}
```

Update `CA-001 US-087: toque em +250ml aumenta consumo em 250` test:

The existing test uses `find.textContaining('250')` — the chip "250 ml" is still visible in the new UI (it's one of the preset chips). The add button now says "+ 250 ml" (via `waterAddCupButton(250)`). The test should still find "250" and work. However, if the tap lands on the chip instead of the add button, it would call `updateCupVolume(250)` rather than `logWater(250)`.

Replace the test to be explicit about tapping the add button:
```dart
testWidgets('CA-001 US-087: botão de adicionar copo aumenta consumo pelo volume atual',
    (tester) async {
  final repo = _FakeNutritionRepository(
    initial: const DailyWaterSummary(
      waterMinimumMl: 2100,
      waterIdealMl: 3500,
      waterConsumedMl: 0,
      cupVolumeMl: 250,
    ),
  );

  await tester.pumpWidget(_buildApp(repo));
  await tester.pumpAndSettle();

  // The add button contains "+ 250 ml" (waterAddCupButton(250)).
  // Find it using the key text from l10n.waterAddCupButton(250) = '+ 250 ml'.
  final addButton = find.widgetWithText(GestureDetector, '+ 250 ml');
  expect(addButton, findsOneWidget);
  await tester.tap(addButton);
  await tester.pumpAndSettle();

  // After adding 250 ml, consumed shows 250 ml.
  expect(find.textContaining('250'), findsWidgets);
});
```

> Note: `waterAddCupButton` produces `+ {amount} ml`. In Portuguese locale the button text is `+ 250 ml`.

- [ ] **Step 2: Create water_cup_volume_flow_test.dart**

```dart
// E2E: US-090 — copos ajustáveis
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/nutrition/domain/entities/daily_water_summary.dart';
import 'package:awaken/features/nutrition/domain/repositories/nutrition_repository.dart';
import 'package:awaken/features/nutrition/presentation/pages/nutrition_page.dart';
import 'package:awaken/features/nutrition/presentation/providers/nutrition_providers.dart';
import 'package:awaken/features/subscriptions/presentation/providers/subscription_status_controller.dart';
import 'package:awaken/features/subscriptions/presentation/providers/subscription_status_state.dart';
import 'package:awaken/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _FakeNutritionRepository implements NutritionRepository {
  _FakeNutritionRepository({required this.initial, this.updateCupVolumeError});

  final DailyWaterSummary initial;
  final Object? updateCupVolumeError;

  late DailyWaterSummary _current = initial;

  @override
  Future<DailyWaterSummary> getBasicNutritionToday() async => _current;

  @override
  Future<DailyWaterSummary> logWaterIntake(int amountMl) async {
    _current = _current.copyWith(
      waterConsumedMl: _current.waterConsumedMl + amountMl,
    );
    return _current;
  }

  @override
  Future<DailyWaterSummary> updateCupVolume(int cupVolumeMl) async {
    if (updateCupVolumeError != null) throw updateCupVolumeError!;
    _current = _current.copyWith(cupVolumeMl: cupVolumeMl);
    return _current;
  }
}

class _FixedStatusController extends SubscriptionStatusController {
  _FixedStatusController(this._status);
  final String _status;

  @override
  SubscriptionStatusState build() =>
      SubscriptionStatusLoaded(accessStatus: _status);

  @override
  Future<void> loadStatus() async {}
}

Widget _buildApp(NutritionRepository repo) {
  return ProviderScope(
    overrides: [
      nutritionRepositoryProvider.overrideWithValue(repo),
      subscriptionStatusControllerProvider
          .overrideWith(() => _FixedStatusController('trial_active')),
    ],
    child: MaterialApp(
      locale: const Locale('pt'),
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: AppLocalizations.supportedLocales,
      home: const NutritionPage(),
    ),
  );
}

void main() {
  group('Cup volume flow — US-090', () {
    testWidgets(
        'CA-001: trocar copo de 250 para 500 ml muda contagem de 4 para 2 copos',
        (tester) async {
      final repo = _FakeNutritionRepository(
        initial: const DailyWaterSummary(
          waterMinimumMl: 2100,
          waterIdealMl: 3500,
          waterConsumedMl: 1000,
          cupVolumeMl: 250,
        ),
      );

      await tester.pumpWidget(_buildApp(repo));
      await tester.pumpAndSettle();

      // With 250 ml cup, 1000 ml = 4 cups.
      expect(find.textContaining('4'), findsWidgets);

      // Tap the 500 ml chip.
      await tester.tap(find.text('500 ml'));
      await tester.pumpAndSettle();

      // With 500 ml cup, 1000 ml = 2 cups.
      expect(find.textContaining('2'), findsWidgets);
    });

    testWidgets(
        'CA-002: botão de adicionar usa volume atual do copo',
        (tester) async {
      final repo = _FakeNutritionRepository(
        initial: const DailyWaterSummary(
          waterMinimumMl: 2100,
          waterIdealMl: 3500,
          waterConsumedMl: 0,
          cupVolumeMl: 300,
        ),
      );

      await tester.pumpWidget(_buildApp(repo));
      await tester.pumpAndSettle();

      // Add button should say "+ 300 ml".
      expect(find.textContaining('300'), findsWidgets);

      // Tap add button.
      final addButton = find.text('+ 300 ml');
      await tester.tap(addButton.first);
      await tester.pumpAndSettle();

      // Consumed should now be 300 ml.
      expect(find.textContaining('300'), findsWidgets);
    });

    testWidgets('RN-002: exibe quantidade de copos com base no volume configurado',
        (tester) async {
      final repo = _FakeNutritionRepository(
        initial: const DailyWaterSummary(
          waterMinimumMl: 2100,
          waterIdealMl: 3500,
          waterConsumedMl: 750,
          cupVolumeMl: 250,
        ),
      );

      await tester.pumpWidget(_buildApp(repo));
      await tester.pumpAndSettle();

      // 750 / 250 = 3 cups.
      expect(find.textContaining('3'), findsWidgets);
    });

    testWidgets('RN-004: chip selecionado é destacado visualmente',
        (tester) async {
      final repo = _FakeNutritionRepository(
        initial: const DailyWaterSummary(
          waterMinimumMl: 2100,
          waterIdealMl: 3500,
          waterConsumedMl: 0,
          cupVolumeMl: 250,
        ),
      );

      await tester.pumpWidget(_buildApp(repo));
      await tester.pumpAndSettle();

      // The "250 ml" chip should exist.
      expect(find.text('250 ml'), findsOneWidget);
    });

    testWidgets('RN-005: erro de rede ao salvar volume não altera estado',
        (tester) async {
      final repo = _FakeNutritionRepository(
        initial: const DailyWaterSummary(
          waterMinimumMl: 2100,
          waterIdealMl: 3500,
          waterConsumedMl: 0,
          cupVolumeMl: 250,
        ),
        updateCupVolumeError: const NetworkError(),
      );

      await tester.pumpWidget(_buildApp(repo));
      await tester.pumpAndSettle();

      await tester.tap(find.text('500 ml'));
      await tester.pumpAndSettle();

      // Volume should remain 250 ml (error reverted state).
      expect(find.textContaining('250 ml'), findsWidgets);
    });

    testWidgets('exibe label de volume do copo (pt-BR)', (tester) async {
      final repo = _FakeNutritionRepository(
        initial: const DailyWaterSummary(
          waterMinimumMl: 2100,
          waterIdealMl: 3500,
          waterConsumedMl: 0,
          cupVolumeMl: 250,
        ),
      );

      await tester.pumpWidget(_buildApp(repo));
      await tester.pumpAndSettle();

      // waterCupVolumeTitle = "VOLUME DO COPO".
      expect(find.textContaining('VOLUME'), findsWidgets);
    });
  });
}
```

- [ ] **Step 3: Run all E2E tests**

```bash
cd apps/mobile && flutter test test/e2e/ -v
```

Expected: all tests PASS.

- [ ] **Step 4: Run all Flutter tests**

```bash
cd apps/mobile && flutter test -v
```

Expected: all tests PASS.

- [ ] **Step 5: Run Flutter analyze**

```bash
cd apps/mobile && flutter analyze
```

Expected: no issues.

---

## Self-Review — Spec Coverage

| Requirement | Task |
|---|---|
| RN-001 Volume do copo configurável | Task 4 (handler), Task 6 (endpoint), Task 14 (UI) |
| RN-002 Recalcula copos ao mudar volume | Task 11 (DailyWaterSummary.cupsConsumed), Task 15 (CA-001 E2E) |
| RN-003 Botão rápido usa volume atual | Task 14 (WaterIntakeButtons.onAddCup), Task 15 (CA-002 E2E) |
| RN-004 Persistir preferência | Task 1–2 (entity + repo), Task 4 (handler), Task 9 (integration test) |
| RN-005 Valores inválidos bloqueados | Task 4 (validator 50–2000 ml), Task 9 (integration test) |
| RN-006 Base em ml | DTOs sempre em ml; cups só na UI |
| CA-001 1000ml / 250ml = 4 copos → / 500ml = 2 copos | Task 11 (cupsConsumed), Task 15 (E2E CA-001) |
| CA-002 Copo de 300 ml → toque adiciona 300 ml | Task 13 (onAddCup), Task 15 (E2E CA-002) |
| Analytics water_cup_volume_changed | Task 13 (controller), Task 16 (unit test) |
| L10n pt-BR, EN, ES, FR | Task 10 (ARB + gen-l10n) + keys já existem |
| Acesso expirado bloqueado | Task 13 (controller reverts to AccessBlocked) |
