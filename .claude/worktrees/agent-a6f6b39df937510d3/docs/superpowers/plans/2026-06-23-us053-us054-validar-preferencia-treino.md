# Implementation Plan — US-053 & US-054 (EPIC-007)

> US-053 — Validar alteração do tipo de treino (P0)
> US-054 — Salvar preferência de tipo de treino (P1)
>
> Stack: Flutter + Riverpod | ASP.NET Core .NET 10 | PostgreSQL + EF Core | MediatR/CQRS | xUnit + FluentAssertions + Moq + Testcontainers | Flutter tests: mocktail
>
> Date: 2026-06-23 · Branch: `docs/epic-007-user-stories`
>
> **DO NOT COMMIT.** All code below is complete — no placeholders.

---

## 0. Summary of scope

The existing `PATCH /api/quests/{questId}/training-type` (US-051) already validates-and-applies atomically. These two stories add:

- **US-053**: a separate **dry-run** endpoint `POST /api/quests/{questId}/validate-training-type-change` that validates access + quest status + type + program and **recalculates XP/duration without persisting**. On the Flutter side it adds an `InvalidTrainingTypeError` mapping, a new `ChangeTrainingTypeInvalidType` state, and fires `workout_type_change_validated` (on success) and `workout_type_change_rejected` (on invalid type/program) as part of the **existing** change flow — we do not call the validate endpoint separately from the sheet; the validate endpoint is the backend contract that the same business rules feed.
- **US-054**: a new persisted entity `UserWorkoutPreference` (one-per-user upsert) and `POST /api/users/me/workout-preferences/training-type` returning `204`. Flutter adds a discrete "Remember this workout type" toggle in the sheet; when ON and the change succeeds, it calls `saveWorkoutPreference`.

Both endpoints sit behind the existing `ActiveAccessMiddleware`, so **403 ACCESS_BLOCKED (RN-007 / RN-006)** is enforced before the handler runs — identical to US-051/US-055. No new access logic required.

The valid type/program sets are shared: types `personalized_individual | regeneration | program`; programs `saitama_path | perfect_2`. These already live in `ChangeTrainingTypeCommandValidator`.

---

## 1. Verification commands

Run after each phase.

```bash
# Backend
cd backend/src && dotnet build
cd backend && dotnet test
dotnet ef migrations add AddUserWorkoutPreferences -p Awaken.Infrastructure -s Awaken.Api

# Flutter
cd apps/mobile && flutter gen-l10n
flutter analyze
flutter test
```

Definition of done (CLAUDE.md): handler + validator, Flutter UI with loading/error/empty states, pt-BR/EN/ES localized, unit + integration tests, analytics fired, logs sanitized.

---

# PART A — US-053 (Validate training-type change)

## A1. Contract DTO — `ValidateTrainingTypeChangeResponse`

**New file:** `backend/src/Awaken.Contracts/Quests/ValidateTrainingTypeChangeResponse.cs`

```csharp
namespace Awaken.Contracts.Quests;

public record ValidateTrainingTypeChangeResponse(
    bool Valid,
    long EstimatedXp,
    int EstimatedDurationMinutes);
```

The request reuses the existing `ChangeTrainingTypeRequest` record (`backend/src/Awaken.Contracts/Quests/ChangeTrainingTypeRequest.cs` — `record ChangeTrainingTypeRequest(string TrainingType, string? ProgramId)`). No new request DTO needed.

## A2. Query + Handler + Validator

US-053 is a read-only validation (no persistence), so it is modelled as a **Query** (`Queries/` folder), mirroring `GetQuestPreview`. It reuses `TrainingTypeTemplates`, `FitnessProfileSnapshot`, and the same XP formula used by `QuestResponseMapper.ToPreviewResponse` (`EstimatedXp = round(durationMinutes * 4.0)`).

**New file:** `backend/src/Awaken.Application/Quests/Queries/ValidateTrainingTypeChange/ValidateTrainingTypeChangeQuery.cs`

```csharp
using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;

public record ValidateTrainingTypeChangeQuery(Guid QuestId, string TrainingType, string? ProgramId)
    : IRequest<ValidateTrainingTypeChangeResponse>;
```

**New file:** `backend/src/Awaken.Application/Quests/Queries/ValidateTrainingTypeChange/ValidateTrainingTypeChangeQueryValidator.cs`

(Identical structure to `ChangeTrainingTypeCommandValidator` — RN-002/RN-003.)

```csharp
using FluentValidation;

namespace Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;

public class ValidateTrainingTypeChangeQueryValidator : AbstractValidator<ValidateTrainingTypeChangeQuery>
{
    private static readonly string[] ValidTrainingTypes =
        ["personalized_individual", "regeneration", "program"];

    private static readonly string[] ValidProgramIds =
        ["saitama_path", "perfect_2"];

    public ValidateTrainingTypeChangeQueryValidator()
    {
        RuleFor(x => x.TrainingType)
            .NotEmpty()
            .Must(t => ValidTrainingTypes.Contains(t))
            .WithMessage($"Tipo de treino inválido. Tipos aceitos: {string.Join(", ", ValidTrainingTypes)}.");

        When(x => x.TrainingType == "program", () =>
        {
            RuleFor(x => x.ProgramId)
                .NotEmpty()
                .WithMessage("ProgramId é obrigatório para o tipo 'program'.")
                .Must(id => ValidProgramIds.Contains(id))
                .WithMessage($"Programa inválido. Programas aceitos: {string.Join(", ", ValidProgramIds)}.");
        });
    }
}
```

**New file:** `backend/src/Awaken.Application/Quests/Queries/ValidateTrainingTypeChange/ValidateTrainingTypeChangeQueryHandler.cs`

This handler mirrors `ChangeTrainingTypeCommandHandler` for ownership/status/type/program rules (RN-001/RN-002/RN-003/RN-004) and generates the workout (RN-004) to recalculate XP/duration (RN-005), but **does NOT call `Update`/`SaveChangesAsync`** — it is a dry-run. XP/duration are computed by parsing the generated JSON's `durationMinutes`, matching `QuestResponseMapper` semantics.

```csharp
using System.Text.Json;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;

public class ValidateTrainingTypeChangeQueryHandler(
    IQuestRepository questRepository,
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IWorkoutGeneratorService workoutGeneratorService,
    ICurrentUserService currentUserService)
    : IRequestHandler<ValidateTrainingTypeChangeQuery, ValidateTrainingTypeChangeResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ValidateTrainingTypeChangeResponse> Handle(
        ValidateTrainingTypeChangeQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.UserId != userId)
            throw new UnauthorizedException("QUEST_NOT_OWNED", "Quest nao pertence ao usuario atual.");

        // RN-001: validacao so e permitida antes de iniciar.
        if (quest.Status is "in_progress" or "completed")
            throw new ConflictException("QUEST_ALREADY_STARTED",
                "A quest ja foi iniciada. Nao e possivel alterar o tipo de treino.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        // RN-002/RN-003/RN-004: gera (sem persistir) o treino compativel com o tipo escolhido.
        var workoutJson = await BuildWorkoutJsonAsync(request, userId, user.PreferredLanguage, cancellationToken);

        // RN-005: recalcula duracao e XP a partir do treino gerado (mesma formula do QuestResponseMapper).
        var estimatedDurationMinutes = ParseDurationMinutes(workoutJson);
        var estimatedXp = (long)Math.Round(estimatedDurationMinutes * 4.0);

        return new ValidateTrainingTypeChangeResponse(
            Valid: true,
            EstimatedXp: estimatedXp,
            EstimatedDurationMinutes: estimatedDurationMinutes);
    }

    private async Task<string> BuildWorkoutJsonAsync(
        ValidateTrainingTypeChangeQuery request, Guid userId, string language, CancellationToken cancellationToken)
    {
        switch (request.TrainingType)
        {
            case "personalized_individual":
                var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundException("UserProfile", userId);
                var progression = await hunterProgressionRepository.GetByUserIdAsync(userId, cancellationToken);
                var fitnessProfileJson = FitnessProfileSnapshot.Build(profile, progression);
                var result = await workoutGeneratorService.GenerateWorkoutJsonAsync(
                    userId, language, fitnessProfileJson, cancellationToken);
                return result.WorkoutJson;

            case "regeneration":
                return TrainingTypeTemplates.RegenerationWorkoutJson(language);

            case "program":
                return request.ProgramId switch
                {
                    "saitama_path" => TrainingTypeTemplates.SaitamaPathWorkoutJson(language),
                    "perfect_2" => TrainingTypeTemplates.Perfect2WorkoutJson(language),
                    _ => throw new ConflictException("INVALID_PROGRAM_ID", $"Programa '{request.ProgramId}' nao reconhecido.")
                };

            default:
                throw new ConflictException("INVALID_TRAINING_TYPE",
                    $"Tipo de treino '{request.TrainingType}' nao reconhecido.");
        }
    }

    private static int ParseDurationMinutes(string? workoutJson)
    {
        if (string.IsNullOrWhiteSpace(workoutJson)) return 0;
        using var doc = JsonDocument.Parse(workoutJson);
        return doc.RootElement.TryGetProperty("durationMinutes", out var prop)
               && prop.TryGetInt32(out var minutes)
            ? minutes
            : 0;
    }
}
```

> Note: `JsonOptions` is declared for parity with `QuestResponseMapper`; the lightweight `JsonDocument` parse used here does not need it, so it is acceptable to drop the field if `dotnet build` warns about it being unused. Keep `JsonDocument` parsing — it avoids depending on `QuestResponseMapper`'s private types.

## A3. Controller endpoint

**Edit:** `backend/src/Awaken.Api/Controllers/V1/QuestsController.cs`

Add the using and the endpoint. Status mapping: validator failures → `422` (ValidationBehavior), `ConflictException` → `409` (INVALID_TRAINING_TYPE / INVALID_PROGRAM_ID / QUEST_ALREADY_STARTED), middleware → `403` ACCESS_BLOCKED.

Add to the using block (after the existing `Queries` usings):

```csharp
using Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;
```

Add this method inside the controller (after `ChangeTrainingType`, before `PatchExercise`):

```csharp
    /// US-053: valida (dry-run) a troca de tipo de treino sem persistir.
    /// Recalcula XP e duracao. 422 tipo/programa invalido (validator);
    /// 409 INVALID_TRAINING_TYPE / INVALID_PROGRAM_ID / QUEST_ALREADY_STARTED;
    /// 403 ACCESS_BLOCKED (ActiveAccessMiddleware).
    [HttpPost("{questId:guid}/validate-training-type-change")]
    public async Task<IActionResult> ValidateTrainingTypeChange(
        Guid questId,
        [FromBody] ChangeTrainingTypeRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ValidateTrainingTypeChangeQuery(questId, request.TrainingType, request.ProgramId), ct);
        return Ok(result);
    }
```

## A4. Backend tests — US-053

**New file:** `backend/tests/Awaken.UnitTests/Quests/ValidateTrainingTypeChangeQueryHandlerTests.cs`

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class ValidateTrainingTypeChangeQueryHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IHunterProgressionRepository> _progressionRepository = new();
    private readonly Mock<IWorkoutGeneratorService> _workoutGenerator = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid QuestId = Guid.NewGuid();

    private const string WorkoutJson = """
    {
      "title": "Daily Quest", "description": "Full body",
      "durationMinutes": 30,
      "exercises": [{ "name": "Squat", "sets": 3, "repsMin": 10 }]
    }
    """;

    public ValidateTrainingTypeChangeQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);

        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _workoutGenerator
            .Setup(g => g.GenerateWorkoutJsonAsync(
                UserId, "pt-BR", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutGenerationResult(
                WorkoutJson, IsPersonalized: true, "catalog_rules", "{}"));
    }

    private ValidateTrainingTypeChangeQueryHandler CreateHandler() => new(
        _questRepository.Object,
        _userRepository.Object,
        _userProfileRepository.Object,
        _progressionRepository.Object,
        _workoutGenerator.Object,
        _currentUserService.Object);

    private Quest BuildPendingQuest()
    {
        var quest = Quest.Create(UserId, DateTime.UtcNow.Date, "pt-BR", "idem");
        quest.AssignWorkout(WorkoutJson);
        return quest;
    }

    // ── CA-001: tipo valido (regeneracao) ─────────────────────────────────────

    [Fact]
    public async Task CA001_ReturnsValid_WithRecalculatedXpAndDuration_ForRegeneration()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(
            new ValidateTrainingTypeChangeQuery(quest.Id, "regeneration", null), CancellationToken.None);

        result.Valid.Should().BeTrue();
        // RegenerationWorkoutJson tem durationMinutes = 20 → XP = 80.
        result.EstimatedDurationMinutes.Should().Be(20);
        result.EstimatedXp.Should().Be(80);
    }

    [Fact]
    public async Task CA001_ReturnsValid_ForSaitamaPath()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(
            new ValidateTrainingTypeChangeQuery(quest.Id, "program", "saitama_path"), CancellationToken.None);

        result.Valid.Should().BeTrue();
        result.EstimatedDurationMinutes.Should().Be(60);  // SaitamaPath durationMinutes = 60.
        result.EstimatedXp.Should().Be(240);
    }

    [Fact]
    public async Task DoesNotPersist_NeverCallsUpdate()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler().Handle(
            new ValidateTrainingTypeChangeQuery(quest.Id, "regeneration", null), CancellationToken.None);

        _questRepository.Verify(r => r.Update(It.IsAny<Quest>()), Times.Never);
        quest.TrainingType.Should().Be("personalized_individual"); // inalterado (default da quest gerada).
    }

    // ── RN-001: quest iniciada ────────────────────────────────────────────────

    [Fact]
    public async Task RN001_Throws_ConflictException_WhenQuestInProgress()
    {
        var quest = BuildPendingQuest();
        quest.Start();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(quest.Id, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "QUEST_ALREADY_STARTED");
    }

    // ── RN-003: programa invalido ─────────────────────────────────────────────

    [Fact]
    public async Task RN003_Throws_ConflictException_WhenProgramIdUnknown()
    {
        var quest = BuildPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(quest.Id, "program", "unknown_program"), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "INVALID_PROGRAM_ID");
    }

    // ── Posse ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Throws_UnauthorizedException_WhenQuestBelongsToAnotherUser()
    {
        var otherQuest = Quest.Create(Guid.NewGuid(), DateTime.UtcNow.Date, "pt-BR", "other");
        otherQuest.AssignWorkout(WorkoutJson);
        _questRepository.Setup(r => r.GetByIdAsync(otherQuest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherQuest);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(otherQuest.Id, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>()
            .Where(e => e.Code == "QUEST_NOT_OWNED");
    }

    [Fact]
    public async Task Throws_NotFoundException_WhenQuestDoesNotExist()
    {
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        await CreateHandler()
            .Invoking(h => h.Handle(
                new ValidateTrainingTypeChangeQuery(QuestId, "regeneration", null), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
```

> Confirm `TrainingTypeTemplates.RegenerationWorkoutJson` has `durationMinutes = 20` and `SaitamaPathWorkoutJson` has `60` (verified in `TrainingTypeTemplates.cs`: lines 23/48 = 20/60; Perfect2 = 45). If a template's duration differs, adjust the expected XP (`duration * 4`).

**New file:** `backend/tests/Awaken.UnitTests/Quests/ValidateTrainingTypeChangeQueryValidatorTests.cs`

```csharp
using Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Quests;

public class ValidateTrainingTypeChangeQueryValidatorTests
{
    private readonly ValidateTrainingTypeChangeQueryValidator _validator = new();

    [Theory]
    [InlineData("personalized_individual")]
    [InlineData("regeneration")]
    public void Accepts_ValidTypesWithoutProgram(string type)
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), type, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_UnknownTrainingType()
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), "free_edit", null));
        result.ShouldHaveValidationErrorFor(x => x.TrainingType);
    }

    [Fact]
    public void Rejects_ProgramTypeWithoutProgramId()
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), "program", null));
        result.ShouldHaveValidationErrorFor(x => x.ProgramId);
    }

    [Theory]
    [InlineData("saitama_path")]
    [InlineData("perfect_2")]
    public void Accepts_ValidProgramIds(string programId)
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), "program", programId));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_InvalidProgramId()
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), "program", "nope"));
        result.ShouldHaveValidationErrorFor(x => x.ProgramId);
    }
}
```

**New file:** `backend/tests/Awaken.IntegrationTests/ValidateTrainingTypeChangeEndpointTests.cs`

Reuses the harness shape of `ChangeTrainingTypeEndpointTests` (Testcontainers Postgres, register → trial → onboarding → seed exercise → generate quest).

```csharp
// US-053: validacao (dry-run) da troca de tipo de treino.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Entities.Exercises;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class ValidateTrainingTypeChangeEndpointTests : IAsyncLifetime
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
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task StartTrialAsync()
    {
        var response = await _client.PostAsync("/api/subscriptions/trial/start", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task CompleteOnboardingAsync()
    {
        var payload = new
        {
            goal = "gain_muscle",
            experienceLevel = "intermediate",
            age = 28,
            heightCm = 175.0,
            weightKg = 82.0,
            biologicalSex = "masculino",
            trainingDuration = "6_12_months",
            availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.EnsureSuccessStatusCode();
    }

    private async Task SeedApprovedExerciseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        if (await db.ExerciseCatalogs.AnyAsync()) return;

        var snapshot = new ExerciseCatalogSnapshot(
            RawImportId: null, ProviderName: "test", ProviderExerciseId: "validate-type-ex001",
            ProviderVersion: null, NamePtBr: "Squat", NameOriginal: "Squat", Slug: "validate-type-ex001",
            DescriptionPtBr: "Agachamento", InstructionsPtBr: ["Desça devagar"],
            InstructionsOriginal: ["Go down slowly"], TipsPtBr: [], ExerciseType: "strength",
            MovementPattern: "squat", MovementFamily: "legs", Mechanic: "compound",
            ForceType: "push", PlaneOfMotion: "sagittal", Laterality: "bilateral",
            BodyPosition: "standing", BenchAngle: null, EquipmentCategory: "bodyweight",
            LoadType: "bodyweight", PrimaryRegion: "lower_body", DifficultyLevel: "intermediate",
            DifficultyRank: 2, TechnicalComplexity: 2, ImpactLevel: 2, Environment: "home",
            RequiredEquipment: [], PrimaryMuscleGroups: ["quadriceps"],
            SecondaryMuscleGroups: ["glutes"], BodyParts: ["legs"], JointStressTags: [],
            ContraindicationTags: [], LimitationBlockTags: [], PainBlockTags: [],
            GoalTags: ["gain_muscle", "strength"], RiskTags: [], AccessibilityTags: [],
            TaxonomySignals: [], MinExperienceLevel: "beginner", SuitableForSedentary: true,
            SuitableForBeginner: true, SuitableForIntermediate: true, SuitableForAdvanced: true,
            IsCompound: true, IsUnilateral: false, IsAssisted: false, IsWeighted: false,
            RegressionExerciseIds: [], ProgressionExerciseIds: [], RelatedExerciseIds: [],
            VideoUrl: "https://video.example/validate-type-ex001", ImageUrl: null, GifUrl: null,
            MediaLicenseInfo: null, SanitizationStatus: "approved",
            IsApprovedForWorkoutGeneration: true, Confidence: "high");

        var exercise = ExerciseCatalog.Create(snapshot);
        exercise.SetAttributeContribution(ExerciseAttributeContribution.CreateAutoGenerated(
            primaryAttribute: "strength", strengthXp: 10, agilityXp: 0, enduranceXp: 0,
            vitalityXp: 0, focusXp: 0, wisdomXp: 1));
        db.ExerciseCatalogs.Add(exercise);
        await db.SaveChangesAsync();
    }

    private async Task<Guid> GenerateQuestAsync()
    {
        var response = await _client.PostAsync("/api/quests/daily/generate", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<QuestResponse>())!.Id;
    }

    // ── CA-001: tipo valido retorna recalculo sem persistir ───────────────────

    [Fact]
    public async Task CA001_Returns200_WithValidTrue_ForRegeneration()
    {
        var token = await RegisterAndGetTokenAsync("vt-regen@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/validate-training-type-change",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateTrainingTypeChangeResponse>();
        body!.Valid.Should().BeTrue();
        body.EstimatedDurationMinutes.Should().BeGreaterThan(0);
        body.EstimatedXp.Should().BeGreaterThan(0);

        // Dry-run: a quest no banco continua nao alterada (status pending, tipo default).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var quest = await db.Quests.AsNoTracking().SingleAsync(q => q.Id == questId);
        quest.Status.Should().Be("pending");
    }

    [Fact]
    public async Task Returns422_WhenTrainingTypeIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("vt-invalid@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/validate-training-type-change",
            new { trainingType = "free_edit" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RN001_Returns409_WhenQuestAlreadyStarted()
    {
        var token = await RegisterAndGetTokenAsync("vt-started@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();
        await CompleteOnboardingAsync();
        await SeedApprovedExerciseAsync();
        var questId = await GenerateQuestAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
            var quest = await db.Quests.SingleAsync(q => q.Id == questId);
            quest.Start();
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{questId}/validate-training-type-change",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("QUEST_ALREADY_STARTED");
    }

    [Fact]
    public async Task RN007_Returns403_WhenTrialExpired()
    {
        var token = await RegisterAndGetTokenAsync("vt-expired@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        var syncPayload = new SyncEntitlementRequest("rc_vt_expired", "pro_access", "monthly", expiresAt);
        await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{Guid.NewGuid()}/validate-training-type-change",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("ACCESS_BLOCKED");
    }

    [Fact]
    public async Task Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{Guid.NewGuid()}/validate-training-type-change",
            new { trainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

## A5. Flutter — US-053

### A5.1 New error type

**Edit:** `apps/mobile/lib/core/errors/app_error.dart` — append before the final no-op section (after `QuestAlreadyStartedError`):

```dart
/// US-053: tipo de treino (ou programa) invalido ao validar/alterar.
/// Backend: 409 INVALID_TRAINING_TYPE ou 409 INVALID_PROGRAM_ID.
final class InvalidTrainingTypeError extends AppError {
  const InvalidTrainingTypeError();
}
```

### A5.2 Map the backend error

**Edit:** `apps/mobile/lib/features/quests/data/datasources/quests_remote_data_source.dart` — inside `_mapError`, add after the `QUEST_ALREADY_STARTED` block:

```dart
    if (e.response?.statusCode == 409 &&
        (code == 'INVALID_TRAINING_TYPE' || code == 'INVALID_PROGRAM_ID')) {
      return const InvalidTrainingTypeError();
    }
```

### A5.3 New state

**Edit:** `apps/mobile/lib/features/quests/presentation/providers/change_training_type_state.dart` — add (after `ChangeTrainingTypeAlreadyStarted`):

```dart
final class ChangeTrainingTypeInvalidType extends ChangeTrainingTypeState {
  const ChangeTrainingTypeInvalidType();
}
```

### A5.4 Controller analytics + new state handling (US-053 + US-054 wiring)

**Edit:** `apps/mobile/lib/features/quests/presentation/providers/change_training_type_controller.dart` — replace the whole file. Adds: `workout_type_change_validated` (on success), `workout_type_change_rejected` (on invalid type), the `InvalidTrainingTypeError` branch, the `rememberPreference` flag that triggers US-054's save, and `workout_type_preference_saved`.

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/analytics/analytics_provider.dart';
import '../../../../core/errors/app_error.dart';
import 'change_training_type_state.dart';
import 'quests_providers.dart';

class ChangeTrainingTypeController extends Notifier<ChangeTrainingTypeState> {
  @override
  ChangeTrainingTypeState build() => const ChangeTrainingTypeIdle();

  Future<void> change(
    String questId,
    String trainingType, {
    String? programId,
    bool rememberPreference = false,
  }) async {
    state = const ChangeTrainingTypeLoading();
    final analytics = ref.read(analyticsServiceProvider);
    final repository = ref.read(questsRepositoryProvider);

    await analytics.logEvent('workout_type_change_started', params: {
      'trainingType': trainingType,
      if (programId != null) 'programId': programId,
    });

    try {
      final preview = await repository.changeTrainingType(
          questId, trainingType, programId: programId);
      state = ChangeTrainingTypeSuccess(preview);

      // US-053: validacao bem-sucedida da troca.
      await analytics.logEvent('workout_type_change_validated', params: {
        'trainingType': trainingType,
        if (programId != null) 'programId': programId,
      });
      await analytics.logEvent('workout_type_changed', params: {
        'trainingType': trainingType,
        if (programId != null) 'programId': programId,
      });

      // US-054: salva preferencia de forma discreta (best-effort, nao bloqueia o fluxo P0).
      if (rememberPreference) {
        try {
          await repository.saveWorkoutPreference(trainingType, programId: programId);
          await analytics.logEvent('workout_type_preference_saved', params: {
            'trainingType': trainingType,
            if (programId != null) 'programId': programId,
          });
        } catch (_) {
          // Falha em salvar preferencia nao reverte a troca de tipo (RN-001 P1).
        }
      }
    } on AccessBlockedError {
      state = const ChangeTrainingTypeAccessBlocked();
      await analytics.logEvent('access_blocked');
    } on QuestAlreadyStartedError {
      state = const ChangeTrainingTypeAlreadyStarted();
      await analytics.logEvent('workout_type_change_failed',
          params: {'reason': 'quest_already_started'});
    } on InvalidTrainingTypeError {
      // US-053: tipo/programa rejeitado pelo backend.
      state = const ChangeTrainingTypeInvalidType();
      await analytics.logEvent('workout_type_change_rejected', params: {
        'trainingType': trainingType,
        if (programId != null) 'programId': programId,
      });
    } on NetworkError {
      state = const ChangeTrainingTypeNetworkError();
      await analytics.logEvent('workout_type_change_failed',
          params: {'reason': 'network_error'});
    } catch (_) {
      state = const ChangeTrainingTypeUnexpectedError();
      await analytics.logEvent('workout_type_change_failed',
          params: {'reason': 'unexpected_error'});
    }
  }

  void reset() => state = const ChangeTrainingTypeIdle();
}

final changeTrainingTypeControllerProvider =
    NotifierProvider<ChangeTrainingTypeController, ChangeTrainingTypeState>(
        ChangeTrainingTypeController.new);
```

### A5.5 Sheet — show invalid-type error

**Edit:** `apps/mobile/lib/features/quests/presentation/widgets/training_type_selector_sheet.dart` — in the error-banner block, add after the `ChangeTrainingTypeAlreadyStarted` banner:

```dart
              if (state is ChangeTrainingTypeInvalidType)
                _ErrorBanner(
                    message: l10n.changeTypeInvalidTypeErrorMessage,
                    icon: Icons.block_outlined),
```

(US-054 toggle changes to this same file are in Part B / B6.)

### A5.6 ARB keys — US-053

Append to each ARB before the closing `}`. The current last entry is `changeTypeUnexpectedErrorMessage` (its `@` meta line has **no trailing comma**). So: add a comma after `"@changeTypeUnexpectedErrorMessage": {...}` then append the new keys. The full US-053 + US-054 ARB block is given together in **B7** to keep the comma handling in one place.

---

# PART B — US-054 (Save workout-type preference)

## B1. Domain entity — `UserWorkoutPreference`

**New file:** `backend/src/Awaken.Domain/Entities/Onboarding/UserWorkoutPreference.cs`

Extends `BaseEntity`; one-per-user (unique `UserId`). Provides a `Create` factory and an `UpdatePreference` mutator for the upsert path. Uses `DateTime.UtcNow` in the domain (consistent with `UserProfile` / `BaseEntity`), while the handler will pass `IDateTimeService.UtcNow` for the explicit `updatedAt`.

```csharp
using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Onboarding;

public class UserWorkoutPreference : BaseEntity
{
    public Guid UserId { get; private set; }
    public string PreferredTrainingType { get; private set; } = null!;
    public string? PreferredProgramId { get; private set; }

    private UserWorkoutPreference() { }

    public static UserWorkoutPreference Create(
        Guid userId,
        string preferredTrainingType,
        string? preferredProgramId,
        DateTime utcNow)
    {
        return new UserWorkoutPreference
        {
            UserId = userId,
            PreferredTrainingType = preferredTrainingType,
            PreferredProgramId = preferredProgramId,
            UpdatedAtUtc = utcNow,
        };
    }

    public void UpdatePreference(
        string preferredTrainingType,
        string? preferredProgramId,
        DateTime utcNow)
    {
        PreferredTrainingType = preferredTrainingType;
        PreferredProgramId = preferredProgramId;
        UpdatedAtUtc = utcNow;
    }
}
```

> `UpdatedAtUtc` has a `protected` setter on `BaseEntity`, so it is settable from the derived entity. The `SaveChangesAsync` override also stamps `UpdatedAtUtc` for `Modified` entities — harmless duplication; the explicit set keeps `updatedAt` meaningful on first insert.

## B2. EF configuration

**New file:** `backend/src/Awaken.Infrastructure/Persistence/Configurations/UserWorkoutPreferenceConfiguration.cs`

Table `user_workout_preferences`, unique index on `UserId` (one-per-user). Auto-discovered by `ApplyConfigurationsFromAssembly`.

```csharp
using Awaken.Domain.Entities.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class UserWorkoutPreferenceConfiguration : IEntityTypeConfiguration<UserWorkoutPreference>
{
    public void Configure(EntityTypeBuilder<UserWorkoutPreference> builder)
    {
        builder.ToTable("user_workout_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.PreferredTrainingType)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(p => p.PreferredProgramId)
            .IsRequired(false)
            .HasMaxLength(64);
    }
}
```

## B3. Repository interface + impl + DbSet + DI

**New file:** `backend/src/Awaken.Domain/Repositories/IUserWorkoutPreferenceRepository.cs`

```csharp
using Awaken.Domain.Common;
using Awaken.Domain.Entities.Onboarding;

namespace Awaken.Domain.Repositories;

public interface IUserWorkoutPreferenceRepository : IRepository<UserWorkoutPreference>
{
    Task<UserWorkoutPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

**New file:** `backend/src/Awaken.Infrastructure/Persistence/Repositories/UserWorkoutPreferenceRepository.cs`

```csharp
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class UserWorkoutPreferenceRepository(AwakenDbContext context) : IUserWorkoutPreferenceRepository
{
    public async Task<UserWorkoutPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.UserWorkoutPreferences.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IEnumerable<UserWorkoutPreference>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.UserWorkoutPreferences.ToListAsync(cancellationToken);

    public async Task AddAsync(UserWorkoutPreference entity, CancellationToken cancellationToken = default) =>
        await context.UserWorkoutPreferences.AddAsync(entity, cancellationToken);

    public void Update(UserWorkoutPreference entity) => context.UserWorkoutPreferences.Update(entity);

    public void Remove(UserWorkoutPreference entity) => context.UserWorkoutPreferences.Remove(entity);

    public async Task<UserWorkoutPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.UserWorkoutPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
```

**Edit:** `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs` — add the DbSet after `UserProfiles`:

```csharp
    public DbSet<UserWorkoutPreference> UserWorkoutPreferences => Set<UserWorkoutPreference>();
```

The `UserWorkoutPreference` type lives in `Awaken.Domain.Entities.Onboarding`, already imported via `using Awaken.Domain.Entities.Onboarding;` (present in the file).

**Edit:** `backend/src/Awaken.Infrastructure/DependencyInjection.cs` — add after the `IUserProfileRepository` registration:

```csharp
        services.AddScoped<IUserWorkoutPreferenceRepository, UserWorkoutPreferenceRepository>();
```

## B4. Command + Handler + Validator

**New file:** `backend/src/Awaken.Application/Users/Commands/SaveWorkoutTypePreference/SaveWorkoutTypePreferenceCommand.cs`

```csharp
using MediatR;

namespace Awaken.Application.Users.Commands.SaveWorkoutTypePreference;

public record SaveWorkoutTypePreferenceCommand(string PreferredTrainingType, string? PreferredProgramId)
    : IRequest;
```

**New file:** `backend/src/Awaken.Application/Users/Commands/SaveWorkoutTypePreference/SaveWorkoutTypePreferenceCommandValidator.cs`

RN-002/RN-003: only valid types and programs; program required when type is `program`. (RN-005 is enforced structurally — there is no field for exercise/volume.)

```csharp
using FluentValidation;

namespace Awaken.Application.Users.Commands.SaveWorkoutTypePreference;

public class SaveWorkoutTypePreferenceCommandValidator : AbstractValidator<SaveWorkoutTypePreferenceCommand>
{
    private static readonly string[] ValidTrainingTypes =
        ["personalized_individual", "regeneration", "program"];

    private static readonly string[] ValidProgramIds =
        ["saitama_path", "perfect_2"];

    public SaveWorkoutTypePreferenceCommandValidator()
    {
        RuleFor(x => x.PreferredTrainingType)
            .NotEmpty()
            .Must(t => ValidTrainingTypes.Contains(t))
            .WithMessage($"Tipo de treino inválido. Tipos aceitos: {string.Join(", ", ValidTrainingTypes)}.");

        When(x => x.PreferredTrainingType == "program", () =>
        {
            RuleFor(x => x.PreferredProgramId)
                .NotEmpty()
                .WithMessage("PreferredProgramId é obrigatório para o tipo 'program'.")
                .Must(id => ValidProgramIds.Contains(id))
                .WithMessage($"Programa inválido. Programas aceitos: {string.Join(", ", ValidProgramIds)}.");
        });
    }
}
```

**New file:** `backend/src/Awaken.Application/Users/Commands/SaveWorkoutTypePreference/SaveWorkoutTypePreferenceCommandHandler.cs`

Upsert: load by userId; create if missing else update. Uses `IDateTimeService.UtcNow`. Access already enforced by middleware (RN-006). For non-`program` types, normalize `PreferredProgramId` to `null` so a leftover program id is never stored when the type is not a program.

```csharp
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Users.Commands.SaveWorkoutTypePreference;

public class SaveWorkoutTypePreferenceCommandHandler(
    IUserWorkoutPreferenceRepository preferenceRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<SaveWorkoutTypePreferenceCommand>
{
    public async Task Handle(SaveWorkoutTypePreferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        // RN-002/RN-003: programId so faz sentido para o tipo 'program'.
        var programId = request.PreferredTrainingType == "program"
            ? request.PreferredProgramId
            : null;

        var existing = await preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

        if (existing is null)
        {
            var preference = UserWorkoutPreference.Create(
                userId, request.PreferredTrainingType, programId, utcNow);
            await preferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            existing.UpdatePreference(request.PreferredTrainingType, programId, utcNow);
            preferenceRepository.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

> `IRequest` (no result) + `IRequestHandler<TCommand>` with `Task Handle(...)` is the MediatR v12 unit-returning shape. If the codebase's MediatR version requires `IRequest<Unit>` / returning `Unit.Value`, follow the pattern already used by `SavePhysicalDataCommand` (which the controller calls and discards). `SavePhysicalDataCommand` returns nothing usable (controller does `await mediator.Send(...)` then `NoContent()`), so mirror that exact command's interface declaration.

## B5. Request DTO + controller endpoint

**New file:** `backend/src/Awaken.Contracts/Users/SaveWorkoutTypePreferenceRequest.cs`

```csharp
namespace Awaken.Contracts.Users;

public record SaveWorkoutTypePreferenceRequest(string PreferredTrainingType, string? PreferredProgramId);
```

**Edit:** `backend/src/Awaken.Api/Controllers/V1/UsersController.cs`

Add usings:

```csharp
using Awaken.Application.Users.Commands.SaveWorkoutTypePreference;
```

(`using Awaken.Contracts.Users;` is already present.)

Add the endpoint (after `UpdateProfile`):

```csharp
    /// US-054: salva preferencia de tipo de treino (upsert, um por usuario).
    /// 204 No Content. Acesso expirado bloqueado por ActiveAccessMiddleware (403 RN-006).
    [HttpPost("me/workout-preferences/training-type")]
    public async Task<IActionResult> SaveWorkoutTypePreference(
        [FromBody] SaveWorkoutTypePreferenceRequest request,
        CancellationToken ct)
    {
        await mediator.Send(new SaveWorkoutTypePreferenceCommand(
            request.PreferredTrainingType,
            request.PreferredProgramId), ct);
        return NoContent();
    }
```

## B6. EF migration

```bash
cd backend
dotnet ef migrations add AddUserWorkoutPreferences -p Awaken.Infrastructure -s Awaken.Api
```

This generates the `user_workout_preferences` table (columns: `Id`, `UserId`, `PreferredTrainingType`, `PreferredProgramId`, `CreatedAtUtc`, `UpdatedAtUtc`, `DeletedAtUtc`, `CreatedByUserId`, `UpdatedByUserId`, `IsDeleted`) with a unique index on `UserId`. Integration tests apply migrations via `Database.MigrateAsync()`, so no manual `database update` is needed for tests.

## B7. Backend tests — US-054

**New file:** `backend/tests/Awaken.UnitTests/Users/SaveWorkoutTypePreferenceCommandHandlerTests.cs`

```csharp
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Users.Commands.SaveWorkoutTypePreference;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Users;

public class SaveWorkoutTypePreferenceCommandHandlerTests
{
    private readonly Mock<IUserWorkoutPreferenceRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);

    public SaveWorkoutTypePreferenceCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(Now);
    }

    private SaveWorkoutTypePreferenceCommandHandler CreateHandler() => new(
        _repository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    // ── CA-001: cria preferencia quando nao existe ────────────────────────────

    [Fact]
    public async Task CA001_Creates_WhenNoExistingPreference()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWorkoutPreference?)null);

        UserWorkoutPreference? added = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()))
            .Callback<UserWorkoutPreference, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new SaveWorkoutTypePreferenceCommand("program", "perfect_2"), CancellationToken.None);

        added.Should().NotBeNull();
        added!.UserId.Should().Be(UserId);
        added.PreferredTrainingType.Should().Be("program");
        added.PreferredProgramId.Should().Be("perfect_2");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Upsert: atualiza preferencia existente ────────────────────────────────

    [Fact]
    public async Task Updates_WhenPreferenceAlreadyExists()
    {
        var existing = UserWorkoutPreference.Create(UserId, "regeneration", null, Now);
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await CreateHandler().Handle(
            new SaveWorkoutTypePreferenceCommand("program", "saitama_path"), CancellationToken.None);

        existing.PreferredTrainingType.Should().Be("program");
        existing.PreferredProgramId.Should().Be("saitama_path");
        _repository.Verify(r => r.Update(existing), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── RN-002/RN-003: programId descartado para tipos nao-programa ───────────

    [Fact]
    public async Task DropsProgramId_WhenTypeIsNotProgram()
    {
        _repository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserWorkoutPreference?)null);

        UserWorkoutPreference? added = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<UserWorkoutPreference>(), It.IsAny<CancellationToken>()))
            .Callback<UserWorkoutPreference, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new SaveWorkoutTypePreferenceCommand("regeneration", "saitama_path"), CancellationToken.None);

        added!.PreferredProgramId.Should().BeNull();
    }
}
```

**New file:** `backend/tests/Awaken.UnitTests/Users/SaveWorkoutTypePreferenceCommandValidatorTests.cs`

```csharp
using Awaken.Application.Users.Commands.SaveWorkoutTypePreference;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Users;

public class SaveWorkoutTypePreferenceCommandValidatorTests
{
    private readonly SaveWorkoutTypePreferenceCommandValidator _validator = new();

    [Theory]
    [InlineData("personalized_individual")]
    [InlineData("regeneration")]
    public void Accepts_NonProgramTypes(string type)
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand(type, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_UnknownType()
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand("free_edit", null));
        result.ShouldHaveValidationErrorFor(x => x.PreferredTrainingType);
    }

    [Fact]
    public void Rejects_ProgramTypeWithoutProgramId()
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand("program", null));
        result.ShouldHaveValidationErrorFor(x => x.PreferredProgramId);
    }

    [Fact]
    public void Rejects_InvalidProgramId()
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand("program", "nope"));
        result.ShouldHaveValidationErrorFor(x => x.PreferredProgramId);
    }

    [Theory]
    [InlineData("saitama_path")]
    [InlineData("perfect_2")]
    public void Accepts_ValidProgram(string programId)
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand("program", programId));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

**New file:** `backend/tests/Awaken.IntegrationTests/SaveWorkoutTypePreferenceEndpointTests.cs`

```csharp
// US-054: salvar preferencia de tipo de treino (upsert, 204).
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class SaveWorkoutTypePreferenceEndpointTests : IAsyncLifetime
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
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task StartTrialAsync()
    {
        var response = await _client.PostAsync("/api/subscriptions/trial/start", null);
        response.EnsureSuccessStatusCode();
    }

    // ── CA-001: salva e persiste ──────────────────────────────────────────────

    [Fact]
    public async Task CA001_Returns204_AndPersistsPreference()
    {
        var token = await RegisterAndGetTokenAsync("pref-save@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "program", preferredProgramId = "perfect_2" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var saved = await db.UserWorkoutPreferences.AsNoTracking().SingleAsync();
        saved.PreferredTrainingType.Should().Be("program");
        saved.PreferredProgramId.Should().Be("perfect_2");
    }

    // ── Upsert: segunda chamada atualiza, nao duplica ─────────────────────────

    [Fact]
    public async Task Upsert_SecondCallUpdatesSingleRow()
    {
        var token = await RegisterAndGetTokenAsync("pref-upsert@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "regeneration" });

        var second = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "program", preferredProgramId = "saitama_path" });

        second.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var rows = await db.UserWorkoutPreferences.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(1);
        rows[0].PreferredTrainingType.Should().Be("program");
        rows[0].PreferredProgramId.Should().Be("saitama_path");
    }

    // ── Validacao: tipo invalido → 422 ────────────────────────────────────────

    [Fact]
    public async Task Returns422_WhenTypeInvalid()
    {
        var token = await RegisterAndGetTokenAsync("pref-invalid@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await StartTrialAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "free_edit" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── RN-006: acesso expirado → 403 ─────────────────────────────────────────

    [Fact]
    public async Task RN006_Returns403_WhenAccessExpired()
    {
        var token = await RegisterAndGetTokenAsync("pref-expired@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        var syncPayload = new SyncEntitlementRequest("rc_pref_expired", "pro_access", "monthly", expiresAt);
        await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["code"].ToString().Should().Be("ACCESS_BLOCKED");
    }

    [Fact]
    public async Task Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/users/me/workout-preferences/training-type",
            new { preferredTrainingType = "regeneration" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

> Verify the `ActiveAccessMiddleware` path matching covers `/api/users/me/workout-preferences/...`. If the middleware allowlist is path-prefix based (most onboarding/profile routes are exempt to let a freshly-registered user finish setup), `me/workout-preferences/training-type` must NOT be in the exempt list so RN-006 returns 403. Check `AccessBlockedEndpointTests` and the middleware's exempt-path set; if `me/profile` is allowlisted by prefix `me/`, narrow the rule or add `workout-preferences` to the protected set. This is the one cross-cutting risk in US-054 — confirm before relying on the 403 test.

## B8. Flutter — US-054

### B8.1 Save-preference DTO

**New file:** `apps/mobile/lib/features/quests/data/dtos/save_workout_preference_request_dto.dart`

```dart
class SaveWorkoutPreferenceRequestDto {
  const SaveWorkoutPreferenceRequestDto({
    required this.preferredTrainingType,
    this.preferredProgramId,
  });

  final String preferredTrainingType;
  final String? preferredProgramId;

  Map<String, dynamic> toJson() => {
        'preferredTrainingType': preferredTrainingType,
        if (preferredProgramId != null) 'preferredProgramId': preferredProgramId,
      };
}
```

### B8.2 Data source method

**Edit:** `apps/mobile/lib/features/quests/data/datasources/quests_remote_data_source.dart` — add the import at the top:

```dart
import '../dtos/save_workout_preference_request_dto.dart';
```

Add the method (after `changeTrainingType`):

```dart
  /// US-054: salva preferencia de tipo de treino para quests futuras (204).
  Future<void> saveWorkoutPreference(
      SaveWorkoutPreferenceRequestDto request) async {
    try {
      await _dio.post(
        '/api/users/me/workout-preferences/training-type',
        data: request.toJson(),
      );
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }
```

### B8.3 Repository interface

**Edit:** `apps/mobile/lib/features/quests/domain/repositories/quests_repository.dart` — add after `changeTrainingType`:

```dart
  /// US-054: salva preferencia de tipo de treino do usuario para quests futuras.
  /// [trainingType]: personalized_individual | regeneration | program
  /// [programId]: obrigatório quando trainingType == "program".
  Future<void> saveWorkoutPreference(String trainingType, {String? programId});
```

### B8.4 Repository impl

**Edit:** `apps/mobile/lib/features/quests/data/repositories/quests_repository_impl.dart` — add the import:

```dart
import '../dtos/save_workout_preference_request_dto.dart';
```

Add the method (after `changeTrainingType`):

```dart
  @override
  Future<void> saveWorkoutPreference(String trainingType, {String? programId}) async {
    await _dataSource.saveWorkoutPreference(
      SaveWorkoutPreferenceRequestDto(
        preferredTrainingType: trainingType,
        preferredProgramId: programId,
      ),
    );
  }
```

### B8.5 Sheet — "Remember this type" toggle

**Edit:** `apps/mobile/lib/features/quests/presentation/widgets/training_type_selector_sheet.dart`.

Add a state field (next to `_selectedProgram`):

```dart
  bool _rememberPreference = false;
```

Pass the flag in `_confirm` — change the `.change(...)` call to:

```dart
    ref.read(changeTrainingTypeControllerProvider.notifier).change(
          widget.questId,
          trainingType,
          programId: programId,
          rememberPreference: _rememberPreference,
        );
```

Add the toggle in the build tree, just before the buttons block (`const SizedBox(height: 20)` that precedes the buttons). Place it inside the `else` of the loading branch is not necessary — render it when not loading by guarding with `if (!isLoading)`:

```dart
              if (!isLoading)
                SwitchListTile.adaptive(
                  contentPadding: EdgeInsets.zero,
                  title: Text(l10n.changeTypeRememberPreferenceToggle,
                      style: textTheme.bodyMedium),
                  value: _rememberPreference,
                  onChanged: (value) =>
                      setState(() => _rememberPreference = value),
                ),
```

> The preference save is best-effort inside the controller (wrapped in its own try/catch). A failure shows no blocking error and does not revert the type change (US-054 RN-001 P1: "no friction on the P0 flow"). The `changeTypePreferenceSaveErrorMessage` / `changeTypePreferenceSavedMessage` keys are provided for an optional non-blocking SnackBar — if you want a toast, the sheet pops on success before a SnackBar could show, so prefer surfacing it from the caller page; otherwise the keys remain available and unused by the sheet itself. Minimum required UI is the toggle.

### B8.6 ARB keys — combined US-053 + US-054 block

The current last key in each ARB is `changeTypeUnexpectedErrorMessage`, and its `@`-meta line ends **without** a trailing comma. For each of the three files, add a comma after that `@`-meta line, then append the block below before the final `}`.

**`apps/mobile/lib/l10n/app_pt.arb`** — change line 768 ending to `... }, ` (add comma) and append:

```json
  "changeTypeInvalidTypeErrorMessage": "Tipo de treino inválido. Escolha uma opção disponível.",
  "@changeTypeInvalidTypeErrorMessage": { "description": "Erro exibido quando o tipo/programa de treino é rejeitado pelo backend (US-053)" },
  "changeTypeRememberPreferenceToggle": "Lembrar esse tipo de treino",
  "@changeTypeRememberPreferenceToggle": { "description": "Toggle para salvar a preferência de tipo de treino (US-054)" },
  "changeTypePreferenceSavedMessage": "Preferência salva.",
  "@changeTypePreferenceSavedMessage": { "description": "Confirmação de preferência salva (US-054)" },
  "changeTypePreferenceSaveErrorMessage": "Não foi possível salvar a preferência.",
  "@changeTypePreferenceSaveErrorMessage": { "description": "Erro ao salvar a preferência de tipo de treino (US-054)" }
```

**`apps/mobile/lib/l10n/app_en.arb`** — add comma after `changeTypeUnexpectedErrorMessage` meta and append:

```json
  "changeTypeInvalidTypeErrorMessage": "Invalid workout type. Please choose an available option.",
  "@changeTypeInvalidTypeErrorMessage": { "description": "Error shown when the workout type/program is rejected by the backend (US-053)" },
  "changeTypeRememberPreferenceToggle": "Remember this workout type",
  "@changeTypeRememberPreferenceToggle": { "description": "Toggle to save the workout type preference (US-054)" },
  "changeTypePreferenceSavedMessage": "Preference saved.",
  "@changeTypePreferenceSavedMessage": { "description": "Preference saved confirmation (US-054)" },
  "changeTypePreferenceSaveErrorMessage": "Couldn't save the preference.",
  "@changeTypePreferenceSaveErrorMessage": { "description": "Error saving the workout type preference (US-054)" }
```

**`apps/mobile/lib/l10n/app_es.arb`** — add comma after `changeTypeUnexpectedErrorMessage` meta and append:

```json
  "changeTypeInvalidTypeErrorMessage": "Tipo de entrenamiento inválido. Por favor, elige una opción disponible.",
  "@changeTypeInvalidTypeErrorMessage": { "description": "Error mostrado cuando el backend rechaza el tipo/programa (US-053)" },
  "changeTypeRememberPreferenceToggle": "Recordar este tipo de entrenamiento",
  "@changeTypeRememberPreferenceToggle": { "description": "Interruptor para guardar la preferencia de tipo (US-054)" },
  "changeTypePreferenceSavedMessage": "Preferencia guardada.",
  "@changeTypePreferenceSavedMessage": { "description": "Confirmación de preferencia guardada (US-054)" },
  "changeTypePreferenceSaveErrorMessage": "No se pudo guardar la preferencia.",
  "@changeTypePreferenceSaveErrorMessage": { "description": "Error al guardar la preferencia de tipo (US-054)" }
```

> If the project also ships `app_fr.arb` (CLAUDE.md lists fr), add the 4 keys there too (FR: "Type d'entraînement invalide. Veuillez choisir une option disponible." / "Mémoriser ce type d'entraînement" / "Préférence enregistrée." / "Impossible d'enregistrer la préférence."). PT/EN/ES are the hard requirement per the prompt; FR keeps `flutter gen-l10n` from emitting untranslated-message warnings if the project enforces them.

Run `flutter gen-l10n` after editing ARBs so `AppLocalizations` exposes the new getters used by the sheet.

## B9. Flutter tests — US-053 & US-054

**Edit:** `apps/mobile/test/features/quests/presentation/providers/change_training_type_controller_test.dart` — extend the existing file. Register a `setUpAll` fallback for `saveWorkoutPreference` so mocktail can stub it, and add tests. Add at the top of `main()` (inside, before `group`):

```dart
  setUpAll(() {
    registerFallbackValue(<String, Object?>{});
  });
```

Add these tests inside the existing `group('ChangeTrainingTypeController', ...)`:

```dart
    // ── US-053: tipo invalido ────────────────────────────────────────────────

    test('US053 — transitions to InvalidType and fires rejected on invalid type',
        () async {
      when(() => mockRepository.changeTrainingType(
            _questId, 'program',
            programId: 'nope',
          )).thenThrow(const InvalidTrainingTypeError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(changeTrainingTypeControllerProvider.notifier)
          .change(_questId, 'program', programId: 'nope');

      expect(container.read(changeTrainingTypeControllerProvider),
          isA<ChangeTrainingTypeInvalidType>());
      verify(() => mockAnalytics.logEvent('workout_type_change_rejected',
          params: any(named: 'params'))).called(1);
    });

    // ── US-053: evento de validacao no sucesso ───────────────────────────────

    test('US053 — fires workout_type_change_validated on success', () async {
      when(() => mockRepository.changeTrainingType(
            _questId, 'regeneration',
            programId: null,
          )).thenAnswer((_) async => _buildPreview());

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(changeTrainingTypeControllerProvider.notifier)
          .change(_questId, 'regeneration');

      verify(() => mockAnalytics.logEvent('workout_type_change_validated',
          params: any(named: 'params'))).called(1);
    });

    // ── US-054: salva preferencia quando toggle ligado ───────────────────────

    test('US054 — saves preference and fires event when rememberPreference is true',
        () async {
      when(() => mockRepository.changeTrainingType(
            _questId, 'program',
            programId: 'perfect_2',
          )).thenAnswer((_) async => _buildPreview(type: TrainingType.program));
      when(() => mockRepository.saveWorkoutPreference(
            'program',
            programId: 'perfect_2',
          )).thenAnswer((_) async {});

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(changeTrainingTypeControllerProvider.notifier)
          .change(_questId, 'program',
              programId: 'perfect_2', rememberPreference: true);

      verify(() => mockRepository.saveWorkoutPreference('program',
          programId: 'perfect_2')).called(1);
      verify(() => mockAnalytics.logEvent('workout_type_preference_saved',
          params: any(named: 'params'))).called(1);
    });

    // ── US-054: nao salva quando toggle desligado ────────────────────────────

    test('US054 — does not save preference when rememberPreference is false',
        () async {
      when(() => mockRepository.changeTrainingType(
            _questId, 'regeneration',
            programId: null,
          )).thenAnswer((_) async => _buildPreview());

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(changeTrainingTypeControllerProvider.notifier)
          .change(_questId, 'regeneration');

      verifyNever(() => mockRepository.saveWorkoutPreference(any(),
          programId: any(named: 'programId')));
    });

    // ── US-054: falha ao salvar preferencia nao reverte sucesso ──────────────

    test('US054 — preference save failure keeps Success state', () async {
      when(() => mockRepository.changeTrainingType(
            _questId, 'regeneration',
            programId: null,
          )).thenAnswer((_) async => _buildPreview());
      when(() => mockRepository.saveWorkoutPreference(
            'regeneration',
            programId: null,
          )).thenThrow(const NetworkError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(changeTrainingTypeControllerProvider.notifier)
          .change(_questId, 'regeneration', rememberPreference: true);

      expect(container.read(changeTrainingTypeControllerProvider),
          isA<ChangeTrainingTypeSuccess>());
    });
```

> The existing test for the success path verifies `workout_type_changed` is called once — that assertion still holds (we kept that event alongside the new `workout_type_change_validated`).

---

## C. Execution order

1. **Backend US-053**: A1 DTO → A2 query/handler/validator → A3 controller → `dotnet build`.
2. **Backend US-054**: B1 entity → B2 config → B3 repo/DbSet/DI → B4 command/handler/validator → B5 DTO/controller → B6 migration → `dotnet build`.
3. **Backend tests**: A4 + B7 → `dotnet test` (Testcontainers needs Docker running; `docker-compose up -d` not required — Testcontainers spins its own).
4. **Flutter US-053**: A5.1–A5.5 → A5.6/B8.6 ARBs → `flutter gen-l10n`.
5. **Flutter US-054**: B8.1–B8.5 wiring → `flutter gen-l10n` (already run) → `flutter analyze`.
6. **Flutter tests**: B9 → `flutter test`.
7. Validate all flows in pt-BR/EN/ES (DoD). **Do not commit.**

## D. Open items to confirm during implementation (do not block)

- **D1 (US-054, highest risk):** Confirm `ActiveAccessMiddleware` does NOT exempt `me/workout-preferences/training-type`, so RN-006 yields 403. Inspect the middleware's exempt-path set (the same one that lets `me/profile/complete-onboarding` through pre-subscription). If `me/` is prefix-exempt, the 403 test (B7 `RN006_Returns403`) will fail and the rule must be tightened. Locate via: `Grep "ActiveAccess" backend/src`.
- **D2 (US-054 MediatR shape):** Match `SaveWorkoutTypePreferenceCommand`'s `IRequest`/`IRequestHandler` declaration to whatever `SavePhysicalDataCommand` uses (void/Unit). If `SavePhysicalDataCommand : IRequest` with `Task Handle`, the code above is correct as-is.
- **D3 (US-053 durations):** Re-confirm `TrainingTypeTemplates` durations (regeneration=20, saitama=60, perfect2=45) feeding the XP assertions in A4. Adjust expected XP = `duration * 4` if a template changed.
- **D4:** Ensure no PII/tokens are logged anywhere added (ADR-015). The new handlers log nothing; controllers are thin. Compliant by construction.
```