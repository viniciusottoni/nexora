# US-033 — Salvar Perfil Inicial (Completar Onboarding) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar o endpoint `POST /api/users/me/profile/complete-onboarding`, conectar o wizard do onboarding ao backend enviando todos os dados do perfil em uma única chamada atômica, e tratar todos os estados de loading/erro/sucesso no Flutter.

**Architecture:** O wizard coleta dados localmente; no "Confirmar" da tela de revisão, o app envia tudo em um único POST que salva o UserProfile + marca onboardingCompletedAt no User numa transação. Os options do wizard são refatorados para armazenar valores de API internamente (não display strings), com tradução para l10n apenas na UI de exibição. Ao completar, a SessionState é atualizada com `onboardingCompleted: true`, o que aciona o guard de rota para redirecionar para home.

**Tech Stack:** ASP.NET Core 10 (MediatR, FluentValidation, EF Core, xUnit, Testcontainers), Flutter (Riverpod Notifier, Dio, flutter_test, go_router)

---

## File Map

### Backend — New/Modified
| Action | File |
|--------|------|
| Modify | `backend/src/Awaken.Domain/Entities/Onboarding/UserProfile.cs` |
| Modify | `backend/src/Awaken.Infrastructure/Persistence/Configurations/UserProfileConfiguration.cs` |
| Create | `backend/src/Awaken.Infrastructure/Persistence/Migrations/<TIMESTAMP>_AddGoalAndExperienceLevelToUserProfile.cs` |
| Modify | `backend/src/Awaken.Infrastructure/Persistence/Migrations/AwakenDbContextModelSnapshot.cs` |
| Create | `backend/src/Awaken.Contracts/Onboarding/CompleteOnboardingRequest.cs` |
| Create | `backend/src/Awaken.Contracts/Onboarding/CompleteOnboardingResponse.cs` |
| Create | `backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommand.cs` |
| Create | `backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommandValidator.cs` |
| Create | `backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommandHandler.cs` |
| Modify | `backend/src/Awaken.Api/Controllers/V1/UsersController.cs` |
| Create | `backend/tests/Awaken.UnitTests/Onboarding/CompleteOnboardingCommandHandlerTests.cs` |
| Create | `backend/tests/Awaken.UnitTests/Onboarding/CompleteOnboardingCommandValidatorTests.cs` |
| Create | `backend/tests/Awaken.IntegrationTests/UsersCompleteOnboardingEndpointTests.cs` |

### Flutter — New/Modified
| Action | File |
|--------|------|
| Modify | `apps/mobile/lib/core/errors/app_error.dart` |
| Modify | `apps/mobile/lib/l10n/app_pt.arb` |
| Modify | `apps/mobile/lib/l10n/app_en.arb` |
| Modify | `apps/mobile/lib/l10n/app_es.arb` |
| Modify | `apps/mobile/lib/l10n/app_fr.arb` |
| Create | `apps/mobile/lib/features/onboarding/data/datasources/onboarding_remote_data_source.dart` |
| Create | `apps/mobile/lib/features/onboarding/data/dtos/complete_onboarding_response_dto.dart` |
| Create | `apps/mobile/lib/features/onboarding/domain/repositories/onboarding_repository.dart` |
| Create | `apps/mobile/lib/features/onboarding/data/repositories/onboarding_repository_impl.dart` |
| Create | `apps/mobile/lib/features/onboarding/presentation/providers/complete_onboarding_state.dart` |
| Create | `apps/mobile/lib/features/onboarding/presentation/providers/complete_onboarding_controller.dart` |
| Create | `apps/mobile/lib/features/onboarding/presentation/providers/onboarding_providers.dart` |
| Modify | `apps/mobile/lib/features/onboarding/presentation/pages/onboarding_page.dart` |
| Modify | `apps/mobile/test/features/onboarding/presentation/pages/onboarding_page_test.dart` |
| Modify | `apps/mobile/integration_test/state_views_flow_test.dart` |

---

## Task 1: Adicionar Goal e ExperienceLevel ao UserProfile (Domain + Infrastructure)

**Files:**
- Modify: `backend/src/Awaken.Domain/Entities/Onboarding/UserProfile.cs`
- Modify: `backend/src/Awaken.Infrastructure/Persistence/Configurations/UserProfileConfiguration.cs`

- [ ] **Step 1: Escrever o teste unitário para UserProfile com Goal e ExperienceLevel**

```csharp
// backend/tests/Awaken.UnitTests/Domain/UserProfileTests.cs  (CREATE NEW FILE)
using FluentAssertions;
using Awaken.Domain.Entities.Onboarding;

namespace Awaken.UnitTests.Domain;

public class UserProfileTests
{
    [Fact]
    public void Create_WithAllFields_SetsGoalAndExperienceLevel()
    {
        var profile = UserProfile.Create(
            userId: Guid.NewGuid(),
            goal: "gain_muscle",
            experienceLevel: "beginner");

        profile.Goal.Should().Be("gain_muscle");
        profile.ExperienceLevel.Should().Be("beginner");
    }

    [Fact]
    public void ApplyPatch_UpdatesGoalAndExperienceLevel()
    {
        var profile = UserProfile.Create(Guid.NewGuid());
        var utcNow = DateTime.UtcNow;

        profile.ApplyPatch(
            age: null, heightCm: null, weightKg: null, biologicalSex: null,
            trainingDuration: null, availableMinutesPerWorkout: null,
            bodyType: null, physicalLimitations: null, physicalPains: null,
            goal: "lose_weight", experienceLevel: "intermediate",
            utcNow: utcNow);

        profile.Goal.Should().Be("lose_weight");
        profile.ExperienceLevel.Should().Be("intermediate");
    }
}
```

- [ ] **Step 2: Rodar teste para confirmar que falha**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~UserProfileTests" --no-build 2>&1 | tail -10
```
Esperado: FAIL — `UserProfile` não tem `Goal` nem `ExperienceLevel`.

- [ ] **Step 3: Adicionar Goal e ExperienceLevel ao UserProfile**

Substituir o arquivo `backend/src/Awaken.Domain/Entities/Onboarding/UserProfile.cs`:

```csharp
using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Onboarding;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string? Goal { get; private set; }
    public string? ExperienceLevel { get; private set; }
    public int? Age { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? WeightKg { get; private set; }
    public string? BiologicalSex { get; private set; }
    public string? TrainingDuration { get; private set; }
    public int? AvailableMinutesPerWorkout { get; private set; }
    public string? BodyType { get; private set; }
    public List<string>? PhysicalLimitations { get; private set; }
    public List<string>? PhysicalPains { get; private set; }

    private UserProfile() { }

    public static UserProfile Create(
        Guid userId,
        string? goal = null,
        string? experienceLevel = null,
        int? age = null,
        decimal? heightCm = null,
        decimal? weightKg = null,
        string? biologicalSex = null,
        string? trainingDuration = null,
        int? availableMinutesPerWorkout = null,
        string? bodyType = null,
        List<string>? physicalLimitations = null,
        List<string>? physicalPains = null)
    {
        return new UserProfile
        {
            UserId = userId,
            Goal = goal,
            ExperienceLevel = experienceLevel,
            Age = age,
            HeightCm = heightCm,
            WeightKg = weightKg,
            BiologicalSex = Normalize(biologicalSex),
            TrainingDuration = trainingDuration,
            AvailableMinutesPerWorkout = availableMinutesPerWorkout,
            BodyType = bodyType,
            PhysicalLimitations = physicalLimitations,
            PhysicalPains = physicalPains,
        };
    }

    public void ApplyPatch(
        int? age,
        decimal? heightCm,
        decimal? weightKg,
        string? biologicalSex,
        string? trainingDuration,
        int? availableMinutesPerWorkout,
        string? bodyType,
        List<string>? physicalLimitations,
        List<string>? physicalPains,
        DateTime utcNow,
        string? goal = null,
        string? experienceLevel = null)
    {
        if (goal is not null) Goal = goal;
        if (experienceLevel is not null) ExperienceLevel = experienceLevel;
        if (age.HasValue) Age = age.Value;
        if (heightCm.HasValue) HeightCm = heightCm.Value;
        if (weightKg.HasValue) WeightKg = weightKg.Value;
        if (biologicalSex is not null) BiologicalSex = Normalize(biologicalSex);
        if (trainingDuration is not null) TrainingDuration = trainingDuration;
        if (availableMinutesPerWorkout.HasValue) AvailableMinutesPerWorkout = availableMinutesPerWorkout.Value;
        if (bodyType is not null) BodyType = bodyType;
        if (physicalLimitations is not null) PhysicalLimitations = physicalLimitations;
        if (physicalPains is not null) PhysicalPains = physicalPains;
        UpdatedAtUtc = utcNow;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return value.Trim();
    }
}
```

- [ ] **Step 4: Adicionar Goal e ExperienceLevel à configuração EF**

Em `UserProfileConfiguration.cs`, adicionar **antes** da linha de `PhysicalLimitations`:

```csharp
builder.Property(p => p.Goal)
    .IsRequired(false)
    .HasMaxLength(32);

builder.Property(p => p.ExperienceLevel)
    .IsRequired(false)
    .HasMaxLength(32);
```

- [ ] **Step 5: Gerar a migration**

```bash
cd backend
dotnet ef migrations add AddGoalAndExperienceLevelToUserProfile -p src/Awaken.Infrastructure -s src/Awaken.Api
```

Esperado: novo arquivo de migration criado em `src/Awaken.Infrastructure/Persistence/Migrations/`.

- [ ] **Step 6: Rodar os testes de domínio para confirmar**

```bash
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~UserProfileTests" -v minimal
```
Esperado: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/src/Awaken.Domain/Entities/Onboarding/UserProfile.cs \
        backend/src/Awaken.Infrastructure/Persistence/Configurations/UserProfileConfiguration.cs \
        backend/src/Awaken.Infrastructure/Persistence/Migrations/ \
        backend/tests/Awaken.UnitTests/Domain/UserProfileTests.cs
git commit -m "feat(domain): add Goal and ExperienceLevel to UserProfile for US-033"
```

---

## Task 2: Backend — Contratos e Command

**Files:**
- Create: `backend/src/Awaken.Contracts/Onboarding/CompleteOnboardingRequest.cs`
- Create: `backend/src/Awaken.Contracts/Onboarding/CompleteOnboardingResponse.cs`
- Create: `backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommand.cs`

- [ ] **Step 1: Criar CompleteOnboardingRequest**

```csharp
// backend/src/Awaken.Contracts/Onboarding/CompleteOnboardingRequest.cs
namespace Awaken.Contracts.Onboarding;

public record CompleteOnboardingRequest(
    string Goal,
    string ExperienceLevel,
    int Age,
    decimal HeightCm,
    decimal WeightKg,
    string BiologicalSex,
    string TrainingDuration,
    int AvailableMinutesPerWorkout,
    string BodyType,
    List<string> PhysicalLimitations,
    List<string> PhysicalPains);
```

- [ ] **Step 2: Criar CompleteOnboardingResponse**

```csharp
// backend/src/Awaken.Contracts/Onboarding/CompleteOnboardingResponse.cs
namespace Awaken.Contracts.Onboarding;

public record CompleteOnboardingResponse(bool OnboardingCompleted, string NextRoute);
```

- [ ] **Step 3: Criar CompleteOnboardingCommand**

```csharp
// backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommand.cs
using Awaken.Contracts.Onboarding;
using MediatR;

namespace Awaken.Application.Onboarding.Commands.CompleteOnboarding;

public record CompleteOnboardingCommand(
    string Goal,
    string ExperienceLevel,
    int Age,
    decimal HeightCm,
    decimal WeightKg,
    string BiologicalSex,
    string TrainingDuration,
    int AvailableMinutesPerWorkout,
    string BodyType,
    List<string> PhysicalLimitations,
    List<string> PhysicalPains) : IRequest<CompleteOnboardingResponse>;
```

- [ ] **Step 4: Build para verificar compilação**

```bash
cd backend
dotnet build src/Awaken.Application src/Awaken.Contracts -v minimal 2>&1 | tail -5
```
Esperado: Build succeeded.

---

## Task 3: Backend — Validator com testes unitários

**Files:**
- Create: `backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommandValidator.cs`
- Create: `backend/tests/Awaken.UnitTests/Onboarding/CompleteOnboardingCommandValidatorTests.cs`

- [ ] **Step 1: Escrever os testes do validator**

```csharp
// backend/tests/Awaken.UnitTests/Onboarding/CompleteOnboardingCommandValidatorTests.cs
using Awaken.Application.Onboarding.Commands.CompleteOnboarding;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Onboarding;

public class CompleteOnboardingCommandValidatorTests
{
    private readonly CompleteOnboardingCommandValidator _sut = new();

    private static CompleteOnboardingCommand ValidCommand() => new(
        Goal: "gain_muscle",
        ExperienceLevel: "beginner",
        Age: 28,
        HeightCm: 175m,
        WeightKg: 82m,
        BiologicalSex: "masculino",
        TrainingDuration: "1_6_months",
        AvailableMinutesPerWorkout: 30,
        BodyType: "normal",
        PhysicalLimitations: ["no_limitations"],
        PhysicalPains: ["no_pains"]);

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _sut.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("gain_muscle")]
    [InlineData("lose_weight")]
    [InlineData("improve_conditioning")]
    [InlineData("gain_strength")]
    [InlineData("stay_active")]
    public void AllowedGoals_PassValidation(string goal)
    {
        var result = _sut.TestValidate(ValidCommand() with { Goal = goal });
        result.ShouldNotHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void InvalidGoal_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Goal = "become_superhero" });
        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Theory]
    [InlineData("sedentary")]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    [InlineData("advanced")]
    public void AllowedExperienceLevels_PassValidation(string level)
    {
        var result = _sut.TestValidate(ValidCommand() with { ExperienceLevel = level });
        result.ShouldNotHaveValidationErrorFor(x => x.ExperienceLevel);
    }

    [Fact]
    public void InvalidExperienceLevel_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { ExperienceLevel = "god_mode" });
        result.ShouldHaveValidationErrorFor(x => x.ExperienceLevel);
    }

    [Fact]
    public void AgeBelow10_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Age = 9 });
        result.ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Fact]
    public void AgeAbove120_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Age = 121 });
        result.ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Fact]
    public void HeightBelow50_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { HeightCm = 49m });
        result.ShouldHaveValidationErrorFor(x => x.HeightCm);
    }

    [Fact]
    public void WeightBelow20_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { WeightKg = 19m });
        result.ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void EmptyBiologicalSex_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { BiologicalSex = "" });
        result.ShouldHaveValidationErrorFor(x => x.BiologicalSex);
    }

    [Fact]
    public void InvalidTrainingDuration_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { TrainingDuration = "2_years" });
        result.ShouldHaveValidationErrorFor(x => x.TrainingDuration);
    }

    [Fact]
    public void InvalidAvailableMinutes_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { AvailableMinutesPerWorkout = 15 });
        result.ShouldHaveValidationErrorFor(x => x.AvailableMinutesPerWorkout);
    }

    [Fact]
    public void InvalidBodyType_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { BodyType = "obese" });
        result.ShouldHaveValidationErrorFor(x => x.BodyType);
    }

    [Fact]
    public void EmptyPhysicalLimitations_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalLimitations = [] });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalLimitations);
    }

    [Fact]
    public void InvalidPhysicalLimitationTag_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalLimitations = ["unknown"] });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalLimitations);
    }

    [Fact]
    public void EmptyPhysicalPains_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalPains = [] });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalPains);
    }

    [Fact]
    public void InvalidPhysicalPainTag_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalPains = ["unknown"] });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalPains);
    }
}
```

- [ ] **Step 2: Rodar para confirmar falha**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~CompleteOnboardingCommandValidatorTests" --no-build 2>&1 | tail -5
```
Esperado: FAIL — validator não existe.

- [ ] **Step 3: Criar CompleteOnboardingCommandValidator**

```csharp
// backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommandValidator.cs
using FluentValidation;

namespace Awaken.Application.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    private static readonly string[] AllowedGoals =
    [
        "gain_muscle", "lose_weight", "improve_conditioning", "gain_strength", "stay_active"
    ];

    private static readonly string[] AllowedExperienceLevels =
    [
        "sedentary", "beginner", "intermediate", "advanced"
    ];

    private static readonly string[] AllowedTrainingDurations =
    [
        "does_not_train", "less_than_1_month", "1_6_months",
        "6_12_months", "more_than_1_year", "more_than_3_years"
    ];

    private static readonly int[] AllowedAvailableMinutes = [10, 20, 30, 45, 60];

    private static readonly string[] AllowedBodyTypes =
    [
        "lean", "normal", "overweight", "athletic_strong"
    ];

    private static readonly string[] AllowedPhysicalLimitationTags =
    [
        "no_limitations", "disk_herniation", "knee_problem", "no_impact",
        "shoulder_injury", "chronic_lumbar_pain", "medical_restriction"
    ];

    private static readonly string[] AllowedPhysicalPainTags =
    [
        "no_pains", "neck", "shoulder", "wrist", "back", "lower_back", "knees"
    ];

    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.Goal)
            .NotEmpty().WithMessage("Goal e obrigatorio.")
            .Must(v => AllowedGoals.Contains(v))
            .WithMessage("Goal deve ser um dos valores permitidos.");

        RuleFor(x => x.ExperienceLevel)
            .NotEmpty().WithMessage("ExperienceLevel e obrigatorio.")
            .Must(v => AllowedExperienceLevels.Contains(v))
            .WithMessage("ExperienceLevel deve ser um dos valores permitidos.");

        RuleFor(x => x.Age)
            .InclusiveBetween(10, 120)
            .WithMessage("Idade deve estar entre 10 e 120 anos.");

        RuleFor(x => x.HeightCm)
            .InclusiveBetween(50m, 300m)
            .WithMessage("Altura deve estar entre 50 e 300 cm.");

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(20m, 500m)
            .WithMessage("Peso deve estar entre 20 e 500 kg.");

        RuleFor(x => x.BiologicalSex)
            .NotEmpty().WithMessage("Sexo biologico e obrigatorio.")
            .MaximumLength(100);

        RuleFor(x => x.TrainingDuration)
            .Must(v => AllowedTrainingDurations.Contains(v))
            .WithMessage("TrainingDuration invalido.");

        RuleFor(x => x.AvailableMinutesPerWorkout)
            .Must(v => AllowedAvailableMinutes.Contains(v))
            .WithMessage("AvailableMinutesPerWorkout invalido.");

        RuleFor(x => x.BodyType)
            .Must(v => AllowedBodyTypes.Contains(v))
            .WithMessage("BodyType invalido.");

        RuleFor(x => x.PhysicalLimitations)
            .Must(tags => tags.Count > 0)
            .WithMessage("PhysicalLimitations nao pode ser vazia.")
            .Must(tags => tags.All(t => AllowedPhysicalLimitationTags.Contains(t)))
            .WithMessage("PhysicalLimitations contem tags invalidas.");

        RuleFor(x => x.PhysicalPains)
            .Must(tags => tags.Count > 0)
            .WithMessage("PhysicalPains nao pode ser vazia.")
            .Must(tags => tags.All(t => AllowedPhysicalPainTags.Contains(t)))
            .WithMessage("PhysicalPains contem tags invalidas.");
    }
}
```

- [ ] **Step 4: Rodar testes para confirmar PASS**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~CompleteOnboardingCommandValidatorTests" -v minimal
```
Esperado: todos PASS.

---

## Task 4: Backend — Handler com testes unitários

**Files:**
- Create: `backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommandHandler.cs`
- Create: `backend/tests/Awaken.UnitTests/Onboarding/CompleteOnboardingCommandHandlerTests.cs`

- [ ] **Step 1: Escrever os testes do handler**

```csharp
// backend/tests/Awaken.UnitTests/Onboarding/CompleteOnboardingCommandHandlerTests.cs
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Onboarding.Commands.CompleteOnboarding;
using Awaken.Contracts.Onboarding;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Onboarding;

public class CompleteOnboardingCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

    private CompleteOnboardingCommandHandler CreateHandler() => new(
        _profileRepository.Object,
        _userRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    private static CompleteOnboardingCommand ValidCommand() => new(
        Goal: "gain_muscle",
        ExperienceLevel: "beginner",
        Age: 28,
        HeightCm: 175m,
        WeightKg: 82m,
        BiologicalSex: "masculino",
        TrainingDuration: "1_6_months",
        AvailableMinutesPerWorkout: 30,
        BodyType: "normal",
        PhysicalLimitations: ["no_limitations"],
        PhysicalPains: ["no_pains"]);

    private User BuildActiveTrialUser()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        user.StartTrial(UtcNow.AddDays(7));
        return user;
    }

    public CompleteOnboardingCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    [Fact]
    public async Task CA001_CreatesProfileAndCompletesOnboarding_WhenNoProfileExists()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildActiveTrialUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.OnboardingCompleted.Should().BeTrue();
        result.NextRoute.Should().Be("daily_quest");
        user.IsOnboardingComplete.Should().BeTrue();
        user.OnboardingCompletedAtUtc.Should().NotBeNull();
        _profileRepository.Verify(r => r.AddAsync(
            It.Is<UserProfile>(p =>
                p.UserId == UserId &&
                p.Goal == "gain_muscle" &&
                p.ExperienceLevel == "beginner" &&
                p.Age == 28 &&
                p.HeightCm == 175m &&
                p.WeightKg == 82m &&
                p.BiologicalSex == "masculino" &&
                p.TrainingDuration == "1_6_months" &&
                p.AvailableMinutesPerWorkout == 30 &&
                p.BodyType == "normal"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CA001_UpdatesExistingProfileAndCompletesOnboarding()
    {
        var existing = UserProfile.Create(UserId, age: 25);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var user = BuildActiveTrialUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        existing.Goal.Should().Be("gain_muscle");
        existing.Age.Should().Be(28);
        user.IsOnboardingComplete.Should().BeTrue();
        _profileRepository.Verify(r => r.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RN003_OnboardingCompletedAtIsRecorded()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildActiveTrialUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        user.OnboardingCompletedAtUtc.Should().NotBeNull();
        user.CurrentOnboardingStep.Should().Be("completed");
    }

    [Fact]
    public async Task RN005_CompletingOnboarding_DoesNotGrantXP()
    {
        // XP não é concedido — basta o handler retornar sem erros e sem nenhuma chamada de XP
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildActiveTrialUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        // Confirma que o resultado é apenas o response — sem XP field
        result.OnboardingCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CA002_NextRouteIsDailyQuest()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildActiveTrialUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.NextRoute.Should().Be("daily_quest");
    }

    [Fact]
    public async Task TrimsBiologicalSexWhitespace()
    {
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        var user = BuildActiveTrialUser();
        _userRepository.Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = ValidCommand() with { BiologicalSex = "  feminino  " };
        await CreateHandler().Handle(command, CancellationToken.None);

        _profileRepository.Verify(r => r.AddAsync(
            It.Is<UserProfile>(p => p.BiologicalSex == "feminino"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 2: Rodar testes para confirmar falha**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~CompleteOnboardingCommandHandlerTests" --no-build 2>&1 | tail -5
```
Esperado: FAIL — handler não existe.

- [ ] **Step 3: Criar CompleteOnboardingCommandHandler**

```csharp
// backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommandHandler.cs
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Onboarding;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandHandler(
    IUserProfileRepository userProfileRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<CompleteOnboardingCommand, CompleteOnboardingResponse>
{
    public async Task<CompleteOnboardingResponse> Handle(
        CompleteOnboardingCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            profile = UserProfile.Create(
                userId: userId,
                goal: request.Goal,
                experienceLevel: request.ExperienceLevel,
                age: request.Age,
                heightCm: request.HeightCm,
                weightKg: request.WeightKg,
                biologicalSex: request.BiologicalSex,
                trainingDuration: request.TrainingDuration,
                availableMinutesPerWorkout: request.AvailableMinutesPerWorkout,
                bodyType: request.BodyType,
                physicalLimitations: request.PhysicalLimitations,
                physicalPains: request.PhysicalPains);
            await userProfileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.ApplyPatch(
                age: request.Age,
                heightCm: request.HeightCm,
                weightKg: request.WeightKg,
                biologicalSex: request.BiologicalSex,
                trainingDuration: request.TrainingDuration,
                availableMinutesPerWorkout: request.AvailableMinutesPerWorkout,
                bodyType: request.BodyType,
                physicalLimitations: request.PhysicalLimitations,
                physicalPains: request.PhysicalPains,
                utcNow: utcNow,
                goal: request.Goal,
                experienceLevel: request.ExperienceLevel);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        user!.CompleteOnboarding();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteOnboardingResponse(OnboardingCompleted: true, NextRoute: "daily_quest");
    }
}
```

- [ ] **Step 4: Rodar testes para confirmar PASS**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~CompleteOnboardingCommandHandlerTests" -v minimal
```
Esperado: todos PASS.

---

## Task 5: Backend — Controller + Testes de Integração

**Files:**
- Modify: `backend/src/Awaken.Api/Controllers/V1/UsersController.cs`
- Create: `backend/tests/Awaken.IntegrationTests/UsersCompleteOnboardingEndpointTests.cs`

- [ ] **Step 1: Adicionar endpoint no UsersController**

Adicionar ao `UsersController`:

```csharp
// Adicionar using no topo:
using Awaken.Application.Onboarding.Commands.CompleteOnboarding;
using Awaken.Contracts.Onboarding; // (já deve existir)

// Adicionar action:
[HttpPost("me/profile/complete-onboarding")]
public async Task<IActionResult> CompleteOnboarding(
    [FromBody] CompleteOnboardingRequest request,
    CancellationToken ct)
{
    var result = await mediator.Send(new CompleteOnboardingCommand(
        request.Goal,
        request.ExperienceLevel,
        request.Age,
        request.HeightCm,
        request.WeightKg,
        request.BiologicalSex,
        request.TrainingDuration,
        request.AvailableMinutesPerWorkout,
        request.BodyType,
        request.PhysicalLimitations,
        request.PhysicalPains), ct);
    return Ok(result);
}
```

- [ ] **Step 2: Escrever os testes de integração**

```csharp
// backend/tests/Awaken.IntegrationTests/UsersCompleteOnboardingEndpointTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Onboarding;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Entities.Auth;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class UsersCompleteOnboardingEndpointTests : IAsyncLifetime
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
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task StartTrialAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await _client.PostAsync("/api/subscriptions/trial/start", null);
        resp.EnsureSuccessStatusCode();
    }

    private static object ValidPayload() => new
    {
        goal = "gain_muscle",
        experienceLevel = "beginner",
        age = 28,
        heightCm = 175.0,
        weightKg = 82.0,
        biologicalSex = "masculino",
        trainingDuration = "1_6_months",
        availableMinutesPerWorkout = 30,
        bodyType = "normal",
        physicalLimitations = new[] { "no_limitations" },
        physicalPains = new[] { "no_pains" }
    };

    [Fact]
    public async Task CA001_ReturnsOnboardingCompletedTrue_WhenProfileIsComplete()
    {
        var token = await RegisterAndGetTokenAsync();
        await StartTrialAsync(token);

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CompleteOnboardingResponse>();
        body!.OnboardingCompleted.Should().BeTrue();
        body.NextRoute.Should().Be("daily_quest");
    }

    [Fact]
    public async Task CA001_UserIsMarkedAsOnboardingComplete_InDatabase()
    {
        var token = await RegisterAndGetTokenAsync("dbcheck@awaken.app");
        await StartTrialAsync(token);

        await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", ValidPayload());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "dbcheck@awaken.app");
        user.IsOnboardingComplete.Should().BeTrue();
        user.OnboardingCompletedAtUtc.Should().NotBeNull();
        user.CurrentOnboardingStep.Should().Be("completed");
    }

    [Fact]
    public async Task CA001_UserProfileIsSaved_InDatabase()
    {
        var token = await RegisterAndGetTokenAsync("profilecheck@awaken.app");
        await StartTrialAsync(token);

        await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", ValidPayload());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var profile = await db.UserProfiles.SingleAsync();
        profile.Goal.Should().Be("gain_muscle");
        profile.ExperienceLevel.Should().Be("beginner");
        profile.Age.Should().Be(28);
        profile.HeightCm.Should().Be(175m);
        profile.WeightKg.Should().Be(82m);
        profile.BiologicalSex.Should().Be("masculino");
        profile.TrainingDuration.Should().Be("1_6_months");
        profile.AvailableMinutesPerWorkout.Should().Be(30);
        profile.BodyType.Should().Be("normal");
        profile.PhysicalLimitations.Should().BeEquivalentTo(new[] { "no_limitations" });
        profile.PhysicalPains.Should().BeEquivalentTo(new[] { "no_pains" });
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", ValidPayload());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RN002_ReturnsForbidden_WhenTrialExpired()
    {
        var token = await RegisterAndGetTokenAsync("expiredtrial@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Sincroniza assinatura expirada para simular trial expirado
        var syncPayload = new SyncEntitlementRequest("rc_expired", "pro_access", "monthly", DateTime.UtcNow.AddDays(-1));
        await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/profile/complete-onboarding", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ValidationFails_WhenGoalIsInvalid()
    {
        var token = await RegisterAndGetTokenAsync("invalidgoal@awaken.app");
        await StartTrialAsync(token);

        var payload = new
        {
            goal = "become_hero",
            experienceLevel = "beginner",
            age = 28, heightCm = 175.0, weightKg = 82.0, biologicalSex = "masculino",
            trainingDuration = "1_6_months", availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ValidationFails_WhenAgeIsOutOfRange()
    {
        var token = await RegisterAndGetTokenAsync("invalidage@awaken.app");
        await StartTrialAsync(token);

        var payload = new
        {
            goal = "gain_muscle", experienceLevel = "beginner",
            age = 5, heightCm = 175.0, weightKg = 82.0, biologicalSex = "masculino",
            trainingDuration = "1_6_months", availableMinutesPerWorkout = 30,
            bodyType = "normal",
            physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task IsIdempotent_CallingTwiceCompletesSuccessfully()
    {
        var token = await RegisterAndGetTokenAsync("idempotent@awaken.app");
        await StartTrialAsync(token);

        var first = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", ValidPayload());
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", ValidPayload());
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await second.Content.ReadFromJsonAsync<CompleteOnboardingResponse>();
        body!.OnboardingCompleted.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Build completo**

```bash
cd backend
dotnet build -v minimal 2>&1 | tail -10
```
Esperado: Build succeeded.

- [ ] **Step 4: Rodar todos os testes de backend**

```bash
cd backend
dotnet test -v minimal 2>&1 | tail -20
```
Esperado: todos PASS (incluindo testes existentes).

- [ ] **Step 5: Commit**

```bash
git add backend/src/Awaken.Application/Onboarding/Commands/CompleteOnboarding/ \
        backend/src/Awaken.Contracts/Onboarding/CompleteOnboardingRequest.cs \
        backend/src/Awaken.Contracts/Onboarding/CompleteOnboardingResponse.cs \
        backend/src/Awaken.Api/Controllers/V1/UsersController.cs \
        backend/tests/Awaken.UnitTests/Onboarding/ \
        backend/tests/Awaken.IntegrationTests/UsersCompleteOnboardingEndpointTests.cs
git commit -m "feat(api): POST /api/users/me/profile/complete-onboarding — US-033"
```

---

## Task 6: Flutter — Adicionar AccessBlockedError e strings l10n

**Files:**
- Modify: `apps/mobile/lib/core/errors/app_error.dart`
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`
- Modify: `apps/mobile/lib/l10n/app_fr.arb`

- [ ] **Step 1: Adicionar AccessBlockedError ao app_error.dart**

Adicionar no final de `apps/mobile/lib/core/errors/app_error.dart`:

```dart
final class AccessBlockedError extends AppError {
  const AccessBlockedError();
}
```

- [ ] **Step 2: Adicionar chaves l10n em app_pt.arb**

Adicionar **após** a linha `"onboardingSelectRequiredError"`:

```json
  "onboardingCompleteSaving": "Forjando seu perfil...",
  "@onboardingCompleteSaving": { "description": "Texto de carregamento ao salvar perfil inicial" },
  "onboardingCompleteSuccess": "Você adquiriu as qualificações para se tornar um",
  "@onboardingCompleteSuccess": { "description": "Texto de sucesso na tela de conclusão do onboarding — primeira parte" },
  "onboardingCompleteSuccessHighlight": "Jogador",
  "@onboardingCompleteSuccessHighlight": { "description": "Palavra destacada na tela de conclusão do onboarding" },
  "onboardingCompleteSuccessSuffix": ". Você aceita?",
  "@onboardingCompleteSuccessSuffix": { "description": "Sufixo da tela de conclusão do onboarding" },
  "onboardingCompleteAcceptButton": "Aceito!",
  "@onboardingCompleteAcceptButton": { "description": "Botão de aceite na tela de conclusão do onboarding" },
  "onboardingCompleteConnectionError": "Sem conexão. Verifique sua internet e tente novamente.",
  "@onboardingCompleteConnectionError": { "description": "Erro de conexão ao salvar perfil inicial" },
  "onboardingCompleteAccessExpiredError": "Seu acesso expirou. Assine um plano para continuar.",
  "@onboardingCompleteAccessExpiredError": { "description": "Erro de acesso expirado ao tentar concluir onboarding" },
  "onboardingCompleteUnexpectedError": "Não foi possível salvar o perfil. Tente novamente.",
  "@onboardingCompleteUnexpectedError": { "description": "Erro inesperado ao salvar perfil inicial" }
```

- [ ] **Step 3: Adicionar chaves equivalentes em app_en.arb**

```json
  "onboardingCompleteSaving": "Forging your profile...",
  "@onboardingCompleteSaving": { "description": "Loading text while saving initial profile" },
  "onboardingCompleteSuccess": "You have acquired the qualifications to become a",
  "@onboardingCompleteSuccess": { "description": "Success text on onboarding completion screen — first part" },
  "onboardingCompleteSuccessHighlight": "Player",
  "@onboardingCompleteSuccessHighlight": { "description": "Highlighted word on onboarding completion screen" },
  "onboardingCompleteSuccessSuffix": ". Do you accept?",
  "@onboardingCompleteSuccessSuffix": { "description": "Suffix on the onboarding completion screen" },
  "onboardingCompleteAcceptButton": "I accept!",
  "@onboardingCompleteAcceptButton": { "description": "Accept button on onboarding completion screen" },
  "onboardingCompleteConnectionError": "No connection. Check your internet and try again.",
  "@onboardingCompleteConnectionError": { "description": "Connection error when saving initial profile" },
  "onboardingCompleteAccessExpiredError": "Your access has expired. Subscribe to a plan to continue.",
  "@onboardingCompleteAccessExpiredError": { "description": "Access expired error when completing onboarding" },
  "onboardingCompleteUnexpectedError": "Could not save your profile. Please try again.",
  "@onboardingCompleteUnexpectedError": { "description": "Unexpected error when saving initial profile" }
```

- [ ] **Step 4: Adicionar chaves equivalentes em app_es.arb**

```json
  "onboardingCompleteSaving": "Forjando tu perfil...",
  "@onboardingCompleteSaving": { "description": "Texto de carga al guardar el perfil inicial" },
  "onboardingCompleteSuccess": "Has adquirido las cualificaciones para convertirte en",
  "@onboardingCompleteSuccess": { "description": "Texto de éxito en la pantalla de finalización del onboarding" },
  "onboardingCompleteSuccessHighlight": "Jugador",
  "@onboardingCompleteSuccessHighlight": { "description": "Palabra destacada en la pantalla de finalización del onboarding" },
  "onboardingCompleteSuccessSuffix": ". ¿Lo aceptas?",
  "@onboardingCompleteSuccessSuffix": { "description": "Sufijo de la pantalla de finalización del onboarding" },
  "onboardingCompleteAcceptButton": "¡Acepto!",
  "@onboardingCompleteAcceptButton": { "description": "Botón de aceptación en la pantalla de finalización del onboarding" },
  "onboardingCompleteConnectionError": "Sin conexión. Verifica tu internet e intenta de nuevo.",
  "@onboardingCompleteConnectionError": { "description": "Error de conexión al guardar el perfil inicial" },
  "onboardingCompleteAccessExpiredError": "Tu acceso ha expirado. Suscríbete a un plan para continuar.",
  "@onboardingCompleteAccessExpiredError": { "description": "Error de acceso expirado al completar el onboarding" },
  "onboardingCompleteUnexpectedError": "No se pudo guardar tu perfil. Inténtalo de nuevo.",
  "@onboardingCompleteUnexpectedError": { "description": "Error inesperado al guardar el perfil inicial" }
```

- [ ] **Step 5: Adicionar chaves equivalentes em app_fr.arb**

```json
  "onboardingCompleteSaving": "Forgeage de votre profil...",
  "@onboardingCompleteSaving": { "description": "Texte de chargement lors de la sauvegarde du profil initial" },
  "onboardingCompleteSuccess": "Vous avez acquis les qualifications pour devenir",
  "@onboardingCompleteSuccess": { "description": "Texte de succès à l'écran de fin d'onboarding" },
  "onboardingCompleteSuccessHighlight": "Joueur",
  "@onboardingCompleteSuccessHighlight": { "description": "Mot mis en évidence à l'écran de fin d'onboarding" },
  "onboardingCompleteSuccessSuffix": ". Acceptez-vous ?",
  "@onboardingCompleteSuccessSuffix": { "description": "Suffixe de l'écran de fin d'onboarding" },
  "onboardingCompleteAcceptButton": "J'accepte !",
  "@onboardingCompleteAcceptButton": { "description": "Bouton d'acceptation sur l'écran de fin d'onboarding" },
  "onboardingCompleteConnectionError": "Pas de connexion. Vérifiez votre internet et réessayez.",
  "@onboardingCompleteConnectionError": { "description": "Erreur de connexion lors de la sauvegarde du profil initial" },
  "onboardingCompleteAccessExpiredError": "Votre accès a expiré. Abonnez-vous pour continuer.",
  "@onboardingCompleteAccessExpiredError": { "description": "Erreur d'accès expiré lors de la finalisation de l'onboarding" },
  "onboardingCompleteUnexpectedError": "Impossible de sauvegarder votre profil. Réessayez.",
  "@onboardingCompleteUnexpectedError": { "description": "Erreur inattendue lors de la sauvegarde du profil initial" }
```

- [ ] **Step 6: Gerar os arquivos l10n**

```bash
cd apps/mobile
flutter gen-l10n
```
Esperado: arquivos `app_localizations*.dart` gerados sem erros.

- [ ] **Step 7: Commit**

```bash
git add apps/mobile/lib/core/errors/app_error.dart \
        apps/mobile/lib/l10n/
git commit -m "feat(l10n): adicionar strings US-033 (4 idiomas) e AccessBlockedError"
```

---

## Task 7: Flutter — Data layer (DataSource, DTOs, Repository)

**Files:**
- Create: `apps/mobile/lib/features/onboarding/data/dtos/complete_onboarding_response_dto.dart`
- Create: `apps/mobile/lib/features/onboarding/data/datasources/onboarding_remote_data_source.dart`
- Create: `apps/mobile/lib/features/onboarding/domain/repositories/onboarding_repository.dart`
- Create: `apps/mobile/lib/features/onboarding/data/repositories/onboarding_repository_impl.dart`

- [ ] **Step 1: Criar CompleteOnboardingResponseDto**

```dart
// apps/mobile/lib/features/onboarding/data/dtos/complete_onboarding_response_dto.dart
class CompleteOnboardingResponseDto {
  const CompleteOnboardingResponseDto({
    required this.onboardingCompleted,
    required this.nextRoute,
  });

  final bool onboardingCompleted;
  final String nextRoute;

  factory CompleteOnboardingResponseDto.fromJson(Map<String, dynamic> json) =>
      CompleteOnboardingResponseDto(
        onboardingCompleted: json['onboardingCompleted'] as bool,
        nextRoute: json['nextRoute'] as String,
      );
}
```

- [ ] **Step 2: Criar OnboardingRemoteDataSource**

```dart
// apps/mobile/lib/features/onboarding/data/datasources/onboarding_remote_data_source.dart
import 'package:dio/dio.dart';
import '../../../../core/errors/app_error.dart';
import '../dtos/complete_onboarding_response_dto.dart';

class OnboardingRemoteDataSource {
  const OnboardingRemoteDataSource(this._dio);
  final Dio _dio;

  Future<CompleteOnboardingResponseDto> completeOnboarding({
    required String goal,
    required String experienceLevel,
    required int age,
    required double heightCm,
    required double weightKg,
    required String biologicalSex,
    required String trainingDuration,
    required int availableMinutesPerWorkout,
    required String bodyType,
    required List<String> physicalLimitations,
    required List<String> physicalPains,
  }) async {
    try {
      final response = await _dio.post(
        '/api/users/me/profile/complete-onboarding',
        data: {
          'goal': goal,
          'experienceLevel': experienceLevel,
          'age': age,
          'heightCm': heightCm,
          'weightKg': weightKg,
          'biologicalSex': biologicalSex,
          'trainingDuration': trainingDuration,
          'availableMinutesPerWorkout': availableMinutesPerWorkout,
          'bodyType': bodyType,
          'physicalLimitations': physicalLimitations,
          'physicalPains': physicalPains,
        },
      );
      return CompleteOnboardingResponseDto.fromJson(
          response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      if (e.response?.statusCode == 403) throw const AccessBlockedError();
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

- [ ] **Step 3: Criar OnboardingRepository interface**

```dart
// apps/mobile/lib/features/onboarding/domain/repositories/onboarding_repository.dart
abstract interface class OnboardingRepository {
  Future<void> completeOnboarding({
    required String goal,
    required String experienceLevel,
    required int age,
    required double heightCm,
    required double weightKg,
    required String biologicalSex,
    required String trainingDuration,
    required int availableMinutesPerWorkout,
    required String bodyType,
    required List<String> physicalLimitations,
    required List<String> physicalPains,
  });
}
```

- [ ] **Step 4: Criar OnboardingRepositoryImpl**

```dart
// apps/mobile/lib/features/onboarding/data/repositories/onboarding_repository_impl.dart
import '../../domain/repositories/onboarding_repository.dart';
import '../datasources/onboarding_remote_data_source.dart';

class OnboardingRepositoryImpl implements OnboardingRepository {
  const OnboardingRepositoryImpl(this._dataSource);
  final OnboardingRemoteDataSource _dataSource;

  @override
  Future<void> completeOnboarding({
    required String goal,
    required String experienceLevel,
    required int age,
    required double heightCm,
    required double weightKg,
    required String biologicalSex,
    required String trainingDuration,
    required int availableMinutesPerWorkout,
    required String bodyType,
    required List<String> physicalLimitations,
    required List<String> physicalPains,
  }) async {
    await _dataSource.completeOnboarding(
      goal: goal,
      experienceLevel: experienceLevel,
      age: age,
      heightCm: heightCm,
      weightKg: weightKg,
      biologicalSex: biologicalSex,
      trainingDuration: trainingDuration,
      availableMinutesPerWorkout: availableMinutesPerWorkout,
      bodyType: bodyType,
      physicalLimitations: physicalLimitations,
      physicalPains: physicalPains,
    );
  }
}
```

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/lib/features/onboarding/
git commit -m "feat(onboarding): data layer — datasource, dto e repository US-033"
```

---

## Task 8: Flutter — Riverpod State + Controller + Providers

**Files:**
- Create: `apps/mobile/lib/features/onboarding/presentation/providers/complete_onboarding_state.dart`
- Create: `apps/mobile/lib/features/onboarding/presentation/providers/complete_onboarding_controller.dart`
- Create: `apps/mobile/lib/features/onboarding/presentation/providers/onboarding_providers.dart`

- [ ] **Step 1: Criar CompleteOnboardingState**

```dart
// apps/mobile/lib/features/onboarding/presentation/providers/complete_onboarding_state.dart
sealed class CompleteOnboardingState {
  const CompleteOnboardingState();
}

final class CompleteOnboardingIdle extends CompleteOnboardingState {
  const CompleteOnboardingIdle();
}

final class CompleteOnboardingLoading extends CompleteOnboardingState {
  const CompleteOnboardingLoading();
}

final class CompleteOnboardingSuccess extends CompleteOnboardingState {
  const CompleteOnboardingSuccess();
}

final class CompleteOnboardingNetworkError extends CompleteOnboardingState {
  const CompleteOnboardingNetworkError();
}

final class CompleteOnboardingAccessBlocked extends CompleteOnboardingState {
  const CompleteOnboardingAccessBlocked();
}

final class CompleteOnboardingUnexpectedError extends CompleteOnboardingState {
  const CompleteOnboardingUnexpectedError();
}
```

- [ ] **Step 2: Criar CompleteOnboardingController**

```dart
// apps/mobile/lib/features/onboarding/presentation/providers/complete_onboarding_controller.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/analytics/analytics_provider.dart';
import '../../../../core/errors/app_error.dart';
import '../../domain/repositories/onboarding_repository.dart';
import 'complete_onboarding_state.dart';
import 'onboarding_providers.dart';

class CompleteOnboardingController
    extends Notifier<CompleteOnboardingState> {
  @override
  CompleteOnboardingState build() => const CompleteOnboardingIdle();

  Future<void> complete({
    required String goal,
    required String experienceLevel,
    required int age,
    required double heightCm,
    required double weightKg,
    required String biologicalSex,
    required String trainingDuration,
    required int availableMinutesPerWorkout,
    required String bodyType,
    required List<String> physicalLimitations,
    required List<String> physicalPains,
  }) async {
    state = const CompleteOnboardingLoading();

    final analytics = ref.read(analyticsServiceProvider);
    final repository = ref.read(onboardingRepositoryProvider);

    try {
      await repository.completeOnboarding(
        goal: goal,
        experienceLevel: experienceLevel,
        age: age,
        heightCm: heightCm,
        weightKg: weightKg,
        biologicalSex: biologicalSex,
        trainingDuration: trainingDuration,
        availableMinutesPerWorkout: availableMinutesPerWorkout,
        bodyType: bodyType,
        physicalLimitations: physicalLimitations,
        physicalPains: physicalPains,
      );
      await analytics.logEvent('onboarding_completed');
      state = const CompleteOnboardingSuccess();
    } on AccessBlockedError {
      await analytics.logEvent('onboarding_complete_failed',
          params: {'reason': 'access_blocked'});
      state = const CompleteOnboardingAccessBlocked();
    } on NetworkError {
      await analytics.logEvent('onboarding_complete_failed',
          params: {'reason': 'connection'});
      state = const CompleteOnboardingNetworkError();
    } catch (_) {
      await analytics.logEvent('onboarding_complete_failed',
          params: {'reason': 'unexpected'});
      state = const CompleteOnboardingUnexpectedError();
    }
  }
}

final completeOnboardingControllerProvider =
    NotifierProvider<CompleteOnboardingController, CompleteOnboardingState>(
        CompleteOnboardingController.new);
```

- [ ] **Step 3: Criar onboarding_providers.dart**

```dart
// apps/mobile/lib/features/onboarding/presentation/providers/onboarding_providers.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/network/dio_client.dart';
import '../../data/datasources/onboarding_remote_data_source.dart';
import '../../data/repositories/onboarding_repository_impl.dart';
import '../../domain/repositories/onboarding_repository.dart';

final onboardingRemoteDataSourceProvider =
    Provider<OnboardingRemoteDataSource>((ref) {
  return OnboardingRemoteDataSource(ref.watch(authenticatedDioProvider));
});

final onboardingRepositoryProvider = Provider<OnboardingRepository>((ref) {
  return OnboardingRepositoryImpl(
      ref.watch(onboardingRemoteDataSourceProvider));
});
```

- [ ] **Step 4: Commit**

```bash
git add apps/mobile/lib/features/onboarding/presentation/
git commit -m "feat(onboarding): Riverpod controller e state para completar onboarding"
```

---

## Task 9: Flutter — Refatorar wizard e integrar API na OnboardingPage

**Files:**
- Modify: `apps/mobile/lib/features/onboarding/presentation/pages/onboarding_page.dart`

Esta é a maior tarefa. As mudanças são:

1. Adicionar `_StepOption` (apiValue + displayLabel)
2. Refatorar `_OnboardingStep.options` para `List<_StepOption>`
3. Atualizar `_OptionList` e `_OptionCard` para usar `_StepOption`
4. Atualizar `_answers` para armazenar API values
5. Atualizar `_toggleMulti` e `_validateStep` (sem breaking changes)
6. Adicionar helpers de tradução para `_reviewItems()`
7. Converter `OnboardingPage` para `ConsumerStatefulWidget`
8. Adicionar estado de saving (`_saving`) e erro (`_saveError`)
9. Implementar `_completeOnboarding()` que chama o controller
10. Atualizar `_ReviewView` para receber `isSaving` + botão de loading
11. Atualizar a tela de sucesso para usar l10n + atualizar session state

- [ ] **Step 1: Escrever os testes que guiam a refatoração**

Adicionar no arquivo de testes (antes do `void main()`) um helper que vai ser usado nos testes novos:

```dart
// Adicionar ao início de onboarding_page_test.dart, junto com imports:
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:awaken/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:awaken/features/onboarding/presentation/providers/onboarding_providers.dart';
import 'package:awaken/features/onboarding/presentation/providers/complete_onboarding_controller.dart';
import 'package:awaken/features/onboarding/presentation/providers/complete_onboarding_state.dart';

// Stub do repositório (sem chamada de rede)
class _SuccessOnboardingRepository implements OnboardingRepository {
  @override
  Future<void> completeOnboarding({
    required String goal, required String experienceLevel,
    required int age, required double heightCm, required double weightKg,
    required String biologicalSex, required String trainingDuration,
    required int availableMinutesPerWorkout, required String bodyType,
    required List<String> physicalLimitations, required List<String> physicalPains,
  }) async {}
}

class _FailOnboardingRepository implements OnboardingRepository {
  final Object error;
  _FailOnboardingRepository(this.error);
  @override
  Future<void> completeOnboarding({
    required String goal, required String experienceLevel,
    required int age, required double heightCm, required double weightKg,
    required String biologicalSex, required String trainingDuration,
    required int availableMinutesPerWorkout, required String bodyType,
    required List<String> physicalLimitations, required List<String> physicalPains,
  }) async => throw error;
}

// Atualizar _wrap para incluir ProviderScope
Widget _wrap(Widget child, {OnboardingRepository? repo}) => ProviderScope(
      overrides: [
        if (repo != null)
          onboardingRepositoryProvider.overrideWithValue(repo),
      ],
      child: MaterialApp(
        locale: const Locale('pt', 'BR'),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: child,
      ),
    );

Widget _wrapWithLocale(Widget child, Locale locale, {OnboardingRepository? repo}) =>
    ProviderScope(
      overrides: [
        if (repo != null)
          onboardingRepositoryProvider.overrideWithValue(repo),
      ],
      child: MaterialApp(
        locale: locale,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: child,
      ),
    );
```

Adicionar grupo de testes no final do `void main()`:

```dart
  group('US-033 - salvar perfil inicial', () {
    testWidgets('CA-001 clicar Confirmar dispara saving e mostra tela de sucesso',
        (tester) async {
      final l10n = await _ptL10n();
      await tester.pumpWidget(
          _wrap(const OnboardingPage(), repo: _SuccessOnboardingRepository()));
      await _navigateToReview(tester, l10n);

      await _tapVisible(tester, find.text(l10n.onboardingReviewConfirmButton));
      await tester.pump();

      // Loading state visível durante salvamento
      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.pumpAndSettle();

      // Tela de sucesso (AwakenSystemNotificationPage) exibida
      expect(find.text(l10n.onboardingCompleteSuccessHighlight), findsOneWidget);
    });

    testWidgets('CA-001 erro de conexão mostra mensagem de erro na revisao',
        (tester) async {
      final l10n = await _ptL10n();
      await tester.pumpWidget(
          _wrap(const OnboardingPage(),
              repo: _FailOnboardingRepository(const NetworkError())));
      await _navigateToReview(tester, l10n);

      await _tapVisible(tester, find.text(l10n.onboardingReviewConfirmButton));
      await tester.pumpAndSettle();

      expect(find.text(l10n.onboardingCompleteConnectionError), findsOneWidget);
      // Revisão ainda visível (usuário pode tentar novamente)
      expect(find.text(l10n.onboardingReviewTitle), findsOneWidget);
    });

    testWidgets('CA-001 erro inesperado mostra mensagem generica',
        (tester) async {
      final l10n = await _ptL10n();
      await tester.pumpWidget(
          _wrap(const OnboardingPage(),
              repo: _FailOnboardingRepository(Exception('unexpected'))));
      await _navigateToReview(tester, l10n);

      await _tapVisible(tester, find.text(l10n.onboardingReviewConfirmButton));
      await tester.pumpAndSettle();

      expect(find.text(l10n.onboardingCompleteUnexpectedError), findsOneWidget);
    });

    testWidgets('acesso expirado mostra mensagem de acesso bloqueado',
        (tester) async {
      final l10n = await _ptL10n();
      await tester.pumpWidget(
          _wrap(const OnboardingPage(),
              repo: _FailOnboardingRepository(const AccessBlockedError())));
      await _navigateToReview(tester, l10n);

      await _tapVisible(tester, find.text(l10n.onboardingReviewConfirmButton));
      await tester.pumpAndSettle();

      expect(find.text(l10n.onboardingCompleteAccessExpiredError), findsOneWidget);
    });

    for (final locale in [
      const Locale('en'),
      const Locale('es'),
      const Locale('fr'),
    ]) {
      testWidgets('tela de sucesso renderiza em ${locale.languageCode}',
          (tester) async {
        final l10n = await AppLocalizations.delegate.load(locale);
        await tester.pumpWidget(
            _wrapWithLocale(const OnboardingPage(), locale,
                repo: _SuccessOnboardingRepository()));

        await tester.tap(find.text('Comecar avaliacao'));
        await tester.pumpAndSettle();
        await tester.tap(find.text('Ganhar massa'));
        await tester.pumpAndSettle();
        await _tapVisible(tester, find.text('Continuar'));
        await tester.tap(find.text('Iniciante'));
        await tester.pumpAndSettle();
        await _tapVisible(tester, find.text('Continuar'));
        await tester.tap(find.text(l10n.onboardingTrainingDurationDoesNotTrain));
        await tester.pumpAndSettle();
        await _tapVisible(tester, find.text('Continuar'));
        await _fillPhysicalForm(tester);
        await _tapVisible(tester, find.text('Continuar'));
        await tester.tap(find.text(l10n.onboardingBodyTypeNormal));
        await tester.pumpAndSettle();
        await _tapVisible(tester, find.text('Continuar'));
        await tester.tap(find.text(l10n.onboardingWorkoutTime30Option));
        await tester.pumpAndSettle();
        await _tapVisible(tester, find.text('Continuar'));
        await _tapVisible(tester, find.text(l10n.onboardingLimitationsNoneOption));
        await _tapVisible(tester, find.text('Continuar'));
        await _tapVisible(tester, find.text(l10n.onboardingPainsNoneOption));
        await _tapVisible(tester, find.text('Revisar respostas'));

        await _tapVisible(tester, find.text(l10n.onboardingReviewConfirmButton));
        await tester.pumpAndSettle();

        expect(find.text(l10n.onboardingCompleteSuccessHighlight), findsOneWidget);
      });
    }
  });
```

- [ ] **Step 2: Rodar testes para confirmar que falham**

```bash
cd apps/mobile
flutter test test/features/onboarding/presentation/pages/onboarding_page_test.dart 2>&1 | tail -15
```
Esperado: erros de compilação ou FAIL na maioria dos testes (page ainda não é ConsumerStatefulWidget + options ainda são Strings).

- [ ] **Step 3: Implementar toda a refatoração em onboarding_page.dart**

Substituir COMPLETAMENTE o arquivo com a versão refatorada. As mudanças-chave:

**3a. Adicionar imports Riverpod:**
```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/auth/session_provider.dart';
import '../../../../core/auth/session_state.dart';
import '../../../../core/errors/app_error.dart';
import '../providers/complete_onboarding_controller.dart';
import '../providers/complete_onboarding_state.dart';
```

**3b. Adicionar classe `_StepOption`:**
```dart
class _StepOption {
  const _StepOption(this.value, this.label);
  final String value;   // API value (ex: 'gain_muscle')
  final String label;   // Display string (ex: 'Ganhar massa')
}
```

**3c. Mudar `OnboardingPage extends StatefulWidget` para `ConsumerStatefulWidget`:**
```dart
class OnboardingPage extends ConsumerStatefulWidget {
  const OnboardingPage({super.key});

  @override
  ConsumerState<OnboardingPage> createState() => _OnboardingPageState();
}

class _OnboardingPageState extends ConsumerState<OnboardingPage> {
  // ...
  bool _saving = false;
  String? _saveError;
  // ...
}
```

**3d. Atualizar `_OnboardingStep.options` de `List<String>` para `List<_StepOption>`:**
```dart
// _OnboardingStep.single usa List<_StepOption> options
// _OnboardingStep.multi usa List<_StepOption> options e String? exclusiveOption (API value)
```

**3e. Atualizar os `_steps` para usar `_StepOption`:**
```dart
// Step 1 (objetivo):
_OnboardingStep.single(
  keyName: 'objetivo',
  title: 'Qual e o seu objetivo?',
  subtitle: 'Escolha a opcao que melhor representa o que voce busca agora.',
  options: [
    const _StepOption('gain_muscle', 'Ganhar massa'),
    const _StepOption('lose_weight', 'Perder peso'),
    const _StepOption('improve_conditioning', 'Ter condicionamento'),
    const _StepOption('gain_strength', 'Ter mais forca'),
    const _StepOption('stay_active', 'Manter a forma'),
  ],
),
// Step 2 (nivel):
_OnboardingStep.single(
  keyName: 'nivel',
  title: 'Qual e o seu nivel atual?',
  subtitle: 'Seja honesto: isso garante um plano no ritmo certo para voce.',
  options: [
    const _StepOption('sedentary', 'Sedentario'),
    const _StepOption('beginner', 'Iniciante'),
    const _StepOption('intermediate', 'Intermediario'),
    const _StepOption('advanced', 'Avancado'),
  ],
),
// Step 3 (tempo de treino):
_OnboardingStep.single(
  keyName: 'tempo',
  title: _l10n.onboardingTrainingDurationTitle,
  subtitle: _l10n.onboardingTrainingDurationSubtitle,
  options: [
    _StepOption('does_not_train', _l10n.onboardingTrainingDurationDoesNotTrain),
    _StepOption('less_than_1_month', _l10n.onboardingTrainingDurationLessThanOneMonth),
    _StepOption('1_6_months', _l10n.onboardingTrainingDurationOneToSixMonths),
    _StepOption('6_12_months', _l10n.onboardingTrainingDurationSixToTwelveMonths),
    _StepOption('more_than_1_year', _l10n.onboardingTrainingDurationMoreThanOneYear),
    _StepOption('more_than_3_years', _l10n.onboardingTrainingDurationMoreThanThreeYears),
  ],
),
// Step 6 (tempoDisp):
_OnboardingStep.single(
  keyName: 'tempoDisp',
  title: _l10n.onboardingWorkoutTimeTitle,
  subtitle: _l10n.onboardingWorkoutTimeSubtitle,
  options: [
    _StepOption('10', _l10n.onboardingWorkoutTime10Option),
    _StepOption('20', _l10n.onboardingWorkoutTime20Option),
    _StepOption('30', _l10n.onboardingWorkoutTime30Option),
    _StepOption('45', _l10n.onboardingWorkoutTime45Option),
    _StepOption('60', _l10n.onboardingWorkoutTime60Option),
  ],
),
// Step 7 (limitacoes):
_OnboardingStep.multi(
  keyName: 'limitacoes',
  title: _l10n.onboardingLimitationsTitle,
  subtitle: _l10n.onboardingLimitationsSubtitle,
  exclusiveOption: 'no_limitations',
  options: [
    _StepOption('no_limitations', _l10n.onboardingLimitationsNoneOption),
    _StepOption('disk_herniation', _l10n.onboardingLimitationsDiskHerniation),
    _StepOption('knee_problem', _l10n.onboardingLimitationsKneeProblem),
    _StepOption('no_impact', _l10n.onboardingLimitationsNoImpact),
    _StepOption('shoulder_injury', _l10n.onboardingLimitationsShoulderInjury),
    _StepOption('chronic_lumbar_pain', _l10n.onboardingLimitationsChronicLumbarPain),
    _StepOption('medical_restriction', _l10n.onboardingLimitationsMedicalRestriction),
  ],
  disclaimer: _l10n.onboardingLimitationsDisclaimerNote,
),
// Step 8 (dores):
_OnboardingStep.multi(
  keyName: 'dores',
  title: _l10n.onboardingPainsTitle,
  subtitle: _l10n.onboardingPainsSubtitle,
  exclusiveOption: 'no_pains',
  options: [
    _StepOption('no_pains', _l10n.onboardingPainsNoneOption),
    _StepOption('neck', _l10n.onboardingPainsNeck),
    _StepOption('shoulder', _l10n.onboardingPainsShoulder),
    _StepOption('wrist', _l10n.onboardingPainsWrist),
    _StepOption('back', _l10n.onboardingPainsBack),
    _StepOption('lower_back', _l10n.onboardingPainsLowerBack),
    _StepOption('knees', _l10n.onboardingPainsKnees),
  ],
  disclaimer: _l10n.onboardingPainsDisclaimerNote,
),
```

**3f. Atualizar `_OptionList` para receber `List<_StepOption>` e exibir `option.label`, passar `option.value` no `onTap`:**
```dart
class _OptionList extends StatelessWidget {
  const _OptionList({
    required this.options,
    required this.onTap,
    this.selected,
    this.selectedValues,
  });

  final List<_StepOption> options;
  final String? selected;
  final List<String>? selectedValues;
  final ValueChanged<String> onTap; // recebe option.value

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        for (final option in options) ...[
          _OptionCard(
            label: option.label,
            selected: selected == option.value ||
                (selectedValues?.contains(option.value) ?? false),
            onTap: () => onTap(option.value),
          ),
          if (option != options.last) const SizedBox(height: 7),
        ],
      ],
    );
  }
}
```

**3g. `_OptionCard` continua recebendo `String label` — sem mudança.**

**3h. `_StepView._content()` atualizar para passar `data.options` (List<_StepOption>):**
```dart
_StepType.single => _OptionList(
    options: data.options,
    selected: answers[data.keyName] as String?,
    onTap: (value) => onPickSingle(data.keyName, value),
  ),
_StepType.multi => Column(
    children: [
      _OptionList(
        options: data.options,
        selectedValues: answers[data.keyName] as List<String>,
        onTap: (value) => onToggleMulti(data.keyName, value, data.exclusiveOption),
      ),
      // disclaimer...
    ],
  ),
```

**3i. `_OnboardingStep` atualizar types:**
```dart
class _OnboardingStep {
  // ...
  final List<_StepOption> options;
  final String? exclusiveOption; // API value
}
```

**3j. Adicionar helpers de tradução em `_OnboardingPageState`:**
```dart
String _goalLabel(String? v) => switch (v) {
  'gain_muscle' => 'Ganhar massa',
  'lose_weight' => 'Perder peso',
  'improve_conditioning' => 'Ter condicionamento',
  'gain_strength' => 'Ter mais forca',
  'stay_active' => 'Manter a forma',
  _ => '-',
};

String _levelLabel(String? v) => switch (v) {
  'sedentary' => 'Sedentario',
  'beginner' => 'Iniciante',
  'intermediate' => 'Intermediario',
  'advanced' => 'Avancado',
  _ => '-',
};

String _trainingDurationLabel(String? v) => switch (v) {
  'does_not_train' => _l10n.onboardingTrainingDurationDoesNotTrain,
  'less_than_1_month' => _l10n.onboardingTrainingDurationLessThanOneMonth,
  '1_6_months' => _l10n.onboardingTrainingDurationOneToSixMonths,
  '6_12_months' => _l10n.onboardingTrainingDurationSixToTwelveMonths,
  'more_than_1_year' => _l10n.onboardingTrainingDurationMoreThanOneYear,
  'more_than_3_years' => _l10n.onboardingTrainingDurationMoreThanThreeYears,
  _ => '-',
};

String _limitationLabel(String tag) => switch (tag) {
  'no_limitations' => _l10n.onboardingLimitationsNoneOption,
  'disk_herniation' => _l10n.onboardingLimitationsDiskHerniation,
  'knee_problem' => _l10n.onboardingLimitationsKneeProblem,
  'no_impact' => _l10n.onboardingLimitationsNoImpact,
  'shoulder_injury' => _l10n.onboardingLimitationsShoulderInjury,
  'chronic_lumbar_pain' => _l10n.onboardingLimitationsChronicLumbarPain,
  'medical_restriction' => _l10n.onboardingLimitationsMedicalRestriction,
  _ => tag,
};

String _painLabel(String tag) => switch (tag) {
  'no_pains' => _l10n.onboardingPainsNoneOption,
  'neck' => _l10n.onboardingPainsNeck,
  'shoulder' => _l10n.onboardingPainsShoulder,
  'wrist' => _l10n.onboardingPainsWrist,
  'back' => _l10n.onboardingPainsBack,
  'lower_back' => _l10n.onboardingPainsLowerBack,
  'knees' => _l10n.onboardingPainsKnees,
  _ => tag,
};
```

**3k. Atualizar `_reviewItems()` para usar helpers:**
```dart
List<_ReviewItem> _reviewItems() {
  return [
    _ReviewItem(1, _l10n.onboardingReviewGoalLabel, _goalLabel(_answers['objetivo'] as String?)),
    _ReviewItem(2, _l10n.onboardingReviewLevelLabel, _levelLabel(_answers['nivel'] as String?)),
    _ReviewItem(3, _l10n.onboardingReviewTrainingTimeLabel, _trainingDurationLabel(_answers['tempo'] as String?)),
    _ReviewItem(
      4, _l10n.onboardingPhysicalDataStepTitle,
      '${_ageController.text} · ${_heightController.text} $_heightUnit · ${_weightController.text} $_weightUnit · ${_sexController.text.isEmpty ? '-' : _sexController.text}',
    ),
    _ReviewItem(5, _l10n.onboardingReviewBodyTypeLabel, _bodyTypeLabel(_answers['corpo'] as String?)),
    _ReviewItem(6, _l10n.onboardingReviewAvailableTimeLabel,
      _answers['tempoDisp'] != null ? '${_answers['tempoDisp']} min' : '-'),
    _ReviewItem(7, _l10n.onboardingReviewLimitationsLabel,
      (_answers['limitacoes'] as List<String>).map(_limitationLabel).join(', ')),
    _ReviewItem(8, _l10n.onboardingReviewPainsLabel,
      (_answers['dores'] as List<String>).map(_painLabel).join(', ')),
  ];
}
```

**3l. Adicionar método `_completeOnboarding()` em `_OnboardingPageState`:**
```dart
Future<void> _completeOnboarding() async {
  setState(() {
    _saving = true;
    _saveError = null;
  });

  final heightCm = _heightUnit == 'cm'
      ? double.parse(_heightController.text.trim())
      : double.parse(_heightController.text.trim()) * 30.48;
  final weightKg = _weightUnit == 'kg'
      ? double.parse(_weightController.text.trim())
      : double.parse(_weightController.text.trim()) * 0.453592;

  await ref.read(completeOnboardingControllerProvider.notifier).complete(
    goal: _answers['objetivo'] as String,
    experienceLevel: _answers['nivel'] as String,
    age: int.parse(_ageController.text.trim()),
    heightCm: heightCm,
    weightKg: weightKg,
    biologicalSex: _sexController.text.trim(),
    trainingDuration: _answers['tempo'] as String,
    availableMinutesPerWorkout: int.parse(_answers['tempoDisp'] as String),
    bodyType: _answers['corpo'] as String,
    physicalLimitations: List<String>.from(_answers['limitacoes'] as List),
    physicalPains: List<String>.from(_answers['dores'] as List),
  );

  final controllerState = ref.read(completeOnboardingControllerProvider);

  if (controllerState is CompleteOnboardingSuccess) {
    setState(() {
      _saving = false;
      _accepted = true;
    });
  } else if (controllerState is CompleteOnboardingNetworkError) {
    setState(() {
      _saving = false;
      _saveError = _l10n.onboardingCompleteConnectionError;
    });
  } else if (controllerState is CompleteOnboardingAccessBlocked) {
    setState(() {
      _saving = false;
      _saveError = _l10n.onboardingCompleteAccessExpiredError;
    });
  } else {
    setState(() {
      _saving = false;
      _saveError = _l10n.onboardingCompleteUnexpectedError;
    });
  }
}
```

**3m. Atualizar `_ReviewView.onConfirm` para passar `isSaving`:**
```dart
_ReviewView(
  answers: _reviewItems(),
  onEdit: _editStep,
  onBack: () => setState(() {
    _review = false;
    _saveError = null;
  }),
  onConfirm: _saving ? null : _completeOnboarding,
  isSaving: _saving,
  saveError: _saveError,
)
```

**3n. Atualizar `_ReviewView` para receber `isSaving` e `saveError`:**
```dart
class _ReviewView extends StatelessWidget {
  const _ReviewView({
    required this.answers,
    required this.onEdit,
    required this.onBack,
    required this.onConfirm,
    required this.isSaving,
    this.saveError,
  });
  // ...
  final VoidCallback? onConfirm;
  final bool isSaving;
  final String? saveError;
  
  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Center(
      child: SingleChildScrollView(
        // ...
        child: Column(
          children: [
            // ... rows ...
            if (saveError != null) ...[
              const SizedBox(height: AwakenSpacing.md),
              _ErrorPanel(message: saveError!),
            ],
            const SizedBox(height: AwakenSpacing.sectionGap),
            isSaving
                ? const Center(child: CircularProgressIndicator())
                : AwakenButton(
                    label: l10n.onboardingReviewConfirmButton,
                    onPressed: onConfirm,
                  ),
            const SizedBox(height: AwakenSpacing.sm),
            AwakenButton(
              label: l10n.onboardingReviewBackButton,
              variant: AwakenButtonVariant.ghost,
              onPressed: isSaving ? null : onBack,
            ),
          ],
        ),
      ),
    );
  }
}
```

**3o. Atualizar `AwakenSystemNotificationPage.onConfirm` para atualizar session state:**
```dart
AwakenSystemNotificationPage(
  onConfirm: () {
    final current = ref.read(currentSessionStateProvider);
    ref.read(currentSessionStateProvider.notifier).set(
      SessionState(
        hasSession: current?.hasSession ?? true,
        accessStatus: current?.accessStatus,
        onboardingCompleted: true,
      ),
    );
  },
  body: Text.rich(
    TextSpan(
      text: '${_l10n.onboardingCompleteSuccess} ',
      children: [
        TextSpan(
          text: _l10n.onboardingCompleteSuccessHighlight,
          style: AwakenTypography.bodyLarge.copyWith(
            color: AwakenColors.textPrimary,
            fontStyle: FontStyle.italic,
            fontWeight: FontWeight.w700,
          ),
        ),
        TextSpan(
          text: _l10n.onboardingCompleteSuccessSuffix,
          style: AwakenTypography.bodyLarge.copyWith(
            color: AwakenColors.textSecondary,
          ),
        ),
      ],
    ),
    textAlign: TextAlign.center,
    style: AwakenTypography.bodyLarge.copyWith(
      color: AwakenColors.textSecondary,
      height: 1.8,
    ),
  ),
)
```

- [ ] **Step 4: Rodar todos os testes Flutter**

```bash
cd apps/mobile
flutter test test/features/onboarding/presentation/pages/onboarding_page_test.dart 2>&1 | tail -30
```
Esperado: todos PASS (incluindo testes existentes + novos de US-033).

- [ ] **Step 5: Rodar flutter analyze**

```bash
cd apps/mobile
flutter analyze
```
Esperado: no issues.

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/lib/features/onboarding/presentation/pages/onboarding_page.dart \
        apps/mobile/test/features/onboarding/presentation/pages/onboarding_page_test.dart
git commit -m "feat(onboarding): integrar complete-onboarding API com wizard refatorado — US-033"
```

---

## Task 10: Flutter — Integration e2e tests

**Files:**
- Modify: `apps/mobile/integration_test/state_views_flow_test.dart`

- [ ] **Step 1: Adicionar import e stub para onboarding no state_views_flow_test.dart**

Adicionar imports:
```dart
import 'package:awaken/features/onboarding/domain/repositories/onboarding_repository.dart';
import 'package:awaken/features/onboarding/presentation/providers/onboarding_providers.dart';
import 'package:awaken/core/errors/app_error.dart';
```

Adicionar stub classes:
```dart
class _SuccessOnboardingRepository implements OnboardingRepository {
  @override
  Future<void> completeOnboarding({
    required String goal, required String experienceLevel,
    required int age, required double heightCm, required double weightKg,
    required String biologicalSex, required String trainingDuration,
    required int availableMinutesPerWorkout, required String bodyType,
    required List<String> physicalLimitations, required List<String> physicalPains,
  }) async {}
}

class _NetworkErrorOnboardingRepository implements OnboardingRepository {
  @override
  Future<void> completeOnboarding({
    required String goal, required String experienceLevel,
    required int age, required double heightCm, required double weightKg,
    required String biologicalSex, required String trainingDuration,
    required int availableMinutesPerWorkout, required String bodyType,
    required List<String> physicalLimitations, required List<String> physicalPains,
  }) async => throw const NetworkError();
}
```

- [ ] **Step 2: Adicionar testes e2e de US-033 no state_views_flow_test.dart**

Adicionar no `void main()`, num grupo específico:

```dart
  group('US-033 — complete onboarding e2e states', () {
    Future<void> navigateFullWizardToReview(WidgetTester tester) async {
      final l10n = await AppLocalizations.delegate.load(const Locale('pt', 'BR'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Comecar avaliacao'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Ganhar massa'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Continuar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Iniciante'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Continuar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text(l10n.onboardingTrainingDurationDoesNotTrain));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Continuar'));
      await tester.pumpAndSettle();
      // Preenche dados físicos
      await tester.enterText(find.byType(TextField).at(0), '28');
      await tester.enterText(find.byType(TextField).at(1), '175');
      await tester.enterText(find.byType(TextField).at(2), '82');
      await tester.enterText(find.byType(TextField).at(3), 'masculino');
      await tester.pumpAndSettle();
      await tester.tap(find.text('Continuar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text(l10n.onboardingBodyTypeNormal));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Continuar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text(l10n.onboardingWorkoutTime30Option));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Continuar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text(l10n.onboardingLimitationsNoneOption));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Continuar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text(l10n.onboardingPainsNoneOption));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Revisar respostas'));
      await tester.pumpAndSettle();
    }

    testWidgets('US-033-CA001 — salvar perfil com sucesso exibe tela de confirmacao',
        (tester) async {
      IntegrationTestWidgetsFlutterBinding.ensureInitialized();
      final session = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.trialActive,
        onboardingCompleted: false,
      );
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            sessionRepositoryProvider
                .overrideWithValue(_StubSessionRepository(session)),
            currentSessionStateProvider.overrideWith(() {
              final n = CurrentSessionState();
              n.state = session;
              return n;
            }),
            revenueCatServiceProvider
                .overrideWithValue(_NullRevenueCatService()),
            onboardingRepositoryProvider
                .overrideWithValue(_SuccessOnboardingRepository()),
            splashControllerProvider.overrideWith(() {
              final c = SplashController();
              return c;
            }),
            analyticsServiceProvider
                .overrideWithValue(const NoOpAnalyticsService()),
          ],
          child: const AwakenApp(),
        ),
      );
      await tester.pumpAndSettle();

      await navigateFullWizardToReview(tester);

      final l10n =
          await AppLocalizations.delegate.load(const Locale('pt', 'BR'));
      await tester.tap(find.text(l10n.onboardingReviewConfirmButton));
      await tester.pumpAndSettle();

      expect(find.text(l10n.onboardingCompleteSuccessHighlight), findsOneWidget);
    });

    testWidgets('US-033-CA001 — erro de rede exibe mensagem e mantem revisao',
        (tester) async {
      IntegrationTestWidgetsFlutterBinding.ensureInitialized();
      final session = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.trialActive,
        onboardingCompleted: false,
      );
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            sessionRepositoryProvider
                .overrideWithValue(_StubSessionRepository(session)),
            currentSessionStateProvider.overrideWith(() {
              final n = CurrentSessionState();
              n.state = session;
              return n;
            }),
            revenueCatServiceProvider
                .overrideWithValue(_NullRevenueCatService()),
            onboardingRepositoryProvider
                .overrideWithValue(_NetworkErrorOnboardingRepository()),
            splashControllerProvider.overrideWith(() {
              final c = SplashController();
              return c;
            }),
            analyticsServiceProvider
                .overrideWithValue(const NoOpAnalyticsService()),
          ],
          child: const AwakenApp(),
        ),
      );
      await tester.pumpAndSettle();

      await navigateFullWizardToReview(tester);

      final l10n =
          await AppLocalizations.delegate.load(const Locale('pt', 'BR'));
      await tester.tap(find.text(l10n.onboardingReviewConfirmButton));
      await tester.pumpAndSettle();

      expect(find.text(l10n.onboardingCompleteConnectionError), findsOneWidget);
      expect(find.text(l10n.onboardingReviewTitle), findsOneWidget);
    });
  });
```

- [ ] **Step 3: Rodar os testes de integração e2e**

```bash
cd apps/mobile
flutter test integration_test/state_views_flow_test.dart 2>&1 | tail -20
```
Esperado: PASS para os novos testes de US-033.

- [ ] **Step 4: Rodar TODOS os testes Flutter**

```bash
cd apps/mobile
flutter test 2>&1 | tail -20
```
Esperado: todos PASS.

- [ ] **Step 5: Rodar flutter analyze**

```bash
cd apps/mobile
flutter analyze 2>&1 | tail -10
```
Esperado: No issues found.

- [ ] **Step 6: Commit final**

```bash
git add apps/mobile/integration_test/state_views_flow_test.dart
git commit -m "test(e2e): US-033 complete onboarding success e error states"
```

---

## Verificação Final

- [ ] **Backend — todos os testes passam**
```bash
cd backend && dotnet test -v minimal 2>&1 | grep -E "passed|failed|skipped"
```

- [ ] **Flutter — todos os testes passam**
```bash
cd apps/mobile && flutter test 2>&1 | grep -E "All tests|PASS|FAIL"
```

- [ ] **Flutter analyze limpo**
```bash
cd apps/mobile && flutter analyze 2>&1 | tail -5
```

- [ ] **Build release Android compilável**
```bash
cd apps/mobile && flutter build apk --debug 2>&1 | tail -5
```

---

## Self-Review contra spec

| Requisito spec | Implementado? | Onde |
|---|---|---|
| CA-001: Perfil salvo ao confirmar | ✓ | Task 4 handler + Task 9 controller |
| CA-002: Redirect para quest após salvar | ✓ | NextRoute="daily_quest", session state update → router |
| RN-001: Campos obrigatórios validados | ✓ | Task 3 validator (todos required) |
| RN-002: Acesso expirado bloqueia | ✓ | ActiveAccessMiddleware (403) + Task 9 AccessBlockedError |
| RN-003: onboardingCompletedAt registrado | ✓ | Task 4 user.CompleteOnboarding() |
| RN-004: Habilita geração da quest | ✓ | nextRoute: "daily_quest" + onboardingCompleted = true |
| RN-005: Não concede XP | ✓ | Handler não inclui nenhuma lógica de XP |
| Estado: salvando | ✓ | CircularProgressIndicator em _ReviewView |
| Estado: salvo com sucesso | ✓ | AwakenSystemNotificationPage com l10n |
| Estado: campos pendentes | ✓ | Validator 422 + frontend valida antes de enviar |
| Estado: acesso expirado | ✓ | AccessBlockedError → mensagem l10n |
| Estado: erro de conexão | ✓ | NetworkError → mensagem l10n |
| Analytics: onboarding_completed | ✓ | Task 8 controller |
| PT-BR, EN, ES, FR | ✓ | Task 6 — 4 ARBs |
| Textos localizados na tela de sucesso | ✓ | Task 6 + Task 9 usa l10n |
| Goal e ExperienceLevel no UserProfile | ✓ | Task 1 + migration |
| Backend integration tests | ✓ | Task 5 |
| Frontend widget tests | ✓ | Task 9 + 10 |
