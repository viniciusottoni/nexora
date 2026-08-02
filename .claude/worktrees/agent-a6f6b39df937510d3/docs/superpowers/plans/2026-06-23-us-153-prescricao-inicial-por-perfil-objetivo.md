# US-153 — Prescrição Inicial por Perfil e Objetivo: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement exercise prescription (sets, repsMin, repsMax, restSeconds, targetRpe) driven by effectiveExperienceLevel + goal, replacing hardcoded values; sedentary/beginner get fixed reps (repsMax=null), intermediate/advanced get a rep range.

**Architecture:** New pure-static domain service `ExercisePrescriptionEngine` (follows the pattern of `ExerciseScoringEngine`) is called once per workout generation; `WorkoutGeneratorService` applies the prescription to all selected exercises and emits `repsMin`/`repsMax` in the workout JSON. The contract `ExerciseDto`, Flutter DTO, and entity all add `repsMax` (nullable). The notification widget conditionally renders range vs fixed format.

**Tech Stack:** C# (.NET 10) · xUnit + FluentAssertions · Flutter + Dart · ARB l10n

---

## File Map

### New files
| File | Responsibility |
|---|---|
| `backend/src/Awaken.Domain/Services/Quests/ExercisePrescriptionEngine.cs` | Prescription tables + `Prescribe(level, goal)` |
| `backend/tests/Awaken.UnitTests/Quests/ExercisePrescriptionEngineTests.cs` | Unit tests for all RN combinations |

### Modified files
| File | Change |
|---|---|
| `backend/src/Awaken.Contracts/Quests/QuestResponse.cs` | `ExerciseDto`: `Reps` → `RepsMin` (int) + `RepsMax` (int?) |
| `backend/src/Awaken.Infrastructure/Services/WorkoutGeneratorService.cs` | Call `ExercisePrescriptionEngine.Prescribe`; emit `repsMin`/`repsMax` |
| `backend/src/Awaken.Application/Quests/Common/QuestResponseMapper.cs` | Parse `repsMin`/`repsMax` with `reps` fallback (backward compat) |
| `backend/tests/Awaken.UnitTests/Quests/GenerateDailyQuestCommandHandlerTests.cs` | Update `WorkoutJson` fixture to use `repsMin` |
| `backend/tests/Awaken.IntegrationTests/QuestGenerationEndpointTests.cs` | Add CA-001/CA-003/CA-004 prescription assertions |
| `apps/mobile/lib/features/quests/data/dtos/quest_response_dto.dart` | `reps` → `repsMin` + `repsMax` (int?) |
| `apps/mobile/lib/features/quests/domain/entities/daily_quest.dart` | `reps` → `repsMin`/`repsMax`; add `repsDisplay` getter |
| `apps/mobile/lib/features/quests/data/repositories/quests_repository_impl.dart` | Map new fields |
| `apps/mobile/lib/features/quests/presentation/widgets/daily_quest_notification.dart` | Use `repsMin`; conditional range display |
| `apps/mobile/lib/l10n/app_pt.arb` | Update notification key placeholder; add range/fixed display keys |
| `apps/mobile/lib/l10n/app_en.arb` | Same |
| `apps/mobile/lib/l10n/app_es.arb` | Same |
| `apps/mobile/lib/l10n/app_fr.arb` | Same |
| `apps/mobile/test/features/quests/data/datasources/quests_remote_data_source_test.dart` | Update `_questBody` fixture |
| `apps/mobile/test/features/quests/data/repositories/quests_repository_impl_test.dart` | Add CA-003 + CA-004 mapper tests |

---

## Prescription Tables (reference for implementation)

### Sedentário — repsMax always null
| Goal | Sets | RepsMin | Rest (s) | RPE |
|---|---|---|---|---|
| gain_muscle / build_muscle | 2 | 10 | 75 | 3-5 |
| gain_strength / more_strength | 2 | 8 | 90 | 3-5 |
| lose_weight / fat_loss | 1 | 12 | 45 | 3-5 |
| improve_conditioning / conditioning | 1 | 12 | 45 | 3-5 |
| _default_ | 1 | 10 | 60 | 3-5 |

### Iniciante — repsMax always null
| Goal | Sets | RepsMin | Rest (s) | RPE |
|---|---|---|---|---|
| gain_muscle / build_muscle | 3 | 12 | 60 | 5-6 |
| gain_strength / more_strength | 3 | 10 | 75 | 5-6 |
| lose_weight / fat_loss | 2 | 15 | 45 | 5-6 |
| improve_conditioning / conditioning | 2 | 15 | 45 | 5-6 |
| _default_ | 2 | 12 | 60 | 5-6 |

### Intermediário — repsMax set (interval within [10, 20])
| Goal | Sets | RepsMin | RepsMax | Rest (s) | RPE |
|---|---|---|---|---|---|
| gain_muscle / build_muscle | 4 | 10 | 15 | 90 | 6-8 |
| gain_strength / more_strength | 4 | 10 | 12 | 150 | 7-8 |
| lose_weight / fat_loss | 3 | 15 | 20 | 60 | 6-8 |
| improve_conditioning / conditioning | 3 | 15 | 20 | 60 | 6-8 |
| _default_ | 3 | 10 | 15 | 90 | 6-8 |

### Avançado — repsMax set (interval within [4, 30])
| Goal | Sets | RepsMin | RepsMax | Rest (s) | RPE |
|---|---|---|---|---|---|
| gain_muscle / build_muscle | 4 | 8 | 12 | 120 | 7-9 |
| gain_strength / more_strength | 5 | 4 | 6 | 180 | 8-9 |
| lose_weight / fat_loss | 4 | 15 | 25 | 60 | 7-8 |
| improve_conditioning / conditioning | 4 | 20 | 30 | 45 | 7-8 |
| _default_ | 4 | 10 | 15 | 120 | 7-9 |

---

## Task 1 — ExercisePrescriptionEngine domain service

**Files:**
- Create: `backend/src/Awaken.Domain/Services/Quests/ExercisePrescriptionEngine.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace Awaken.Domain.Services.Quests;

/// <summary>
/// US-153: prescribes sets, reps and rest per user level and goal.
/// RN-007: sedentary/beginner get a fixed rep count (RepsMax = null).
/// RN-007: intermediate/advanced get a rep range [RepsMin, RepsMax].
/// </summary>
public static class ExercisePrescriptionEngine
{
    public static ExercisePrescription Prescribe(string effectiveExperienceLevel, string? goal)
    {
        var level = effectiveExperienceLevel.ToLowerInvariant();
        var normalizedGoal = (goal ?? string.Empty).ToLowerInvariant();

        return level switch
        {
            "beginner"     => PrescribeForBeginner(normalizedGoal),
            "intermediate" => PrescribeForIntermediate(normalizedGoal),
            "advanced"     => PrescribeForAdvanced(normalizedGoal),
            _              => PrescribeForSedentary(normalizedGoal), // sedentary + unknown → most conservative
        };
    }

    private static ExercisePrescription PrescribeForSedentary(string goal) => goal switch
    {
        "gain_muscle" or "build_muscle"             => new(Sets: 2, RepsMin: 10, RepsMax: null, RestSeconds: 75,  TargetRpe: "3-5"),
        "gain_strength" or "more_strength"          => new(Sets: 2, RepsMin: 8,  RepsMax: null, RestSeconds: 90,  TargetRpe: "3-5"),
        "lose_weight" or "fat_loss"                 => new(Sets: 1, RepsMin: 12, RepsMax: null, RestSeconds: 45,  TargetRpe: "3-5"),
        "improve_conditioning" or "conditioning"    => new(Sets: 1, RepsMin: 12, RepsMax: null, RestSeconds: 45,  TargetRpe: "3-5"),
        _                                           => new(Sets: 1, RepsMin: 10, RepsMax: null, RestSeconds: 60,  TargetRpe: "3-5"),
    };

    private static ExercisePrescription PrescribeForBeginner(string goal) => goal switch
    {
        "gain_muscle" or "build_muscle"             => new(Sets: 3, RepsMin: 12, RepsMax: null, RestSeconds: 60,  TargetRpe: "5-6"),
        "gain_strength" or "more_strength"          => new(Sets: 3, RepsMin: 10, RepsMax: null, RestSeconds: 75,  TargetRpe: "5-6"),
        "lose_weight" or "fat_loss"                 => new(Sets: 2, RepsMin: 15, RepsMax: null, RestSeconds: 45,  TargetRpe: "5-6"),
        "improve_conditioning" or "conditioning"    => new(Sets: 2, RepsMin: 15, RepsMax: null, RestSeconds: 45,  TargetRpe: "5-6"),
        _                                           => new(Sets: 2, RepsMin: 12, RepsMax: null, RestSeconds: 60,  TargetRpe: "5-6"),
    };

    private static ExercisePrescription PrescribeForIntermediate(string goal) => goal switch
    {
        "gain_muscle" or "build_muscle"             => new(Sets: 4, RepsMin: 10, RepsMax: 15,   RestSeconds: 90,  TargetRpe: "6-8"),
        "gain_strength" or "more_strength"          => new(Sets: 4, RepsMin: 10, RepsMax: 12,   RestSeconds: 150, TargetRpe: "7-8"),
        "lose_weight" or "fat_loss"                 => new(Sets: 3, RepsMin: 15, RepsMax: 20,   RestSeconds: 60,  TargetRpe: "6-8"),
        "improve_conditioning" or "conditioning"    => new(Sets: 3, RepsMin: 15, RepsMax: 20,   RestSeconds: 60,  TargetRpe: "6-8"),
        _                                           => new(Sets: 3, RepsMin: 10, RepsMax: 15,   RestSeconds: 90,  TargetRpe: "6-8"),
    };

    private static ExercisePrescription PrescribeForAdvanced(string goal) => goal switch
    {
        "gain_muscle" or "build_muscle"             => new(Sets: 4, RepsMin: 8,  RepsMax: 12,   RestSeconds: 120, TargetRpe: "7-9"),
        "gain_strength" or "more_strength"          => new(Sets: 5, RepsMin: 4,  RepsMax: 6,    RestSeconds: 180, TargetRpe: "8-9"),
        "lose_weight" or "fat_loss"                 => new(Sets: 4, RepsMin: 15, RepsMax: 25,   RestSeconds: 60,  TargetRpe: "7-8"),
        "improve_conditioning" or "conditioning"    => new(Sets: 4, RepsMin: 20, RepsMax: 30,   RestSeconds: 45,  TargetRpe: "7-8"),
        _                                           => new(Sets: 4, RepsMin: 10, RepsMax: 15,   RestSeconds: 120, TargetRpe: "7-9"),
    };
}

public record ExercisePrescription(
    int Sets,
    int RepsMin,
    int? RepsMax,
    int RestSeconds,
    string TargetRpe);
```

- [ ] **Step 2: Build to confirm no compile errors**

```bash
cd backend/src && dotnet build Awaken.Domain/Awaken.Domain.csproj
```

Expected: `Build succeeded.`

---

## Task 2 — Unit tests for ExercisePrescriptionEngine

**Files:**
- Create: `backend/tests/Awaken.UnitTests/Quests/ExercisePrescriptionEngineTests.cs`

- [ ] **Step 1: Write failing tests first**

```csharp
using Awaken.Domain.Services.Quests;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

public class ExercisePrescriptionEngineTests
{
    // RN-007: sedentary and beginner always return null RepsMax
    [Theory]
    [InlineData("sedentary")]
    [InlineData("beginner")]
    public void RN007_SedentaryAndBeginner_RepsMaxIsNull(string level)
    {
        foreach (var goal in AllGoals())
        {
            var p = ExercisePrescriptionEngine.Prescribe(level, goal);
            p.RepsMax.Should().BeNull($"level={level} goal={goal}");
        }
    }

    // RN-007: intermediate and advanced always have RepsMax > RepsMin
    [Theory]
    [InlineData("intermediate")]
    [InlineData("advanced")]
    public void RN007_IntermediateAndAdvanced_RepsMaxIsGreaterThanRepsMin(string level)
    {
        foreach (var goal in AllGoals())
        {
            var p = ExercisePrescriptionEngine.Prescribe(level, goal);
            p.RepsMax.Should().NotBeNull($"level={level} goal={goal}");
            p.RepsMax!.Value.Should().BeGreaterThan(p.RepsMin, $"level={level} goal={goal}");
        }
    }

    // RN-001: sedentary band: 1-2 sets, 6-12 reps, 45-90s rest, RPE 3-5
    [Fact]
    public void RN001_Sedentary_ParametersWithinBand()
    {
        foreach (var goal in AllGoals())
        {
            var p = ExercisePrescriptionEngine.Prescribe("sedentary", goal);
            p.Sets.Should().BeInRange(1, 2, $"goal={goal}");
            p.RepsMin.Should().BeInRange(6, 12, $"goal={goal}");
            p.RestSeconds.Should().BeInRange(45, 90, $"goal={goal}");
            p.TargetRpe.Should().Be("3-5");
        }
    }

    // RN-002: beginner band: 2-3 sets, 8-15 reps, 45-90s rest, RPE 5-6
    [Fact]
    public void RN002_Beginner_ParametersWithinBand()
    {
        foreach (var goal in AllGoals())
        {
            var p = ExercisePrescriptionEngine.Prescribe("beginner", goal);
            p.Sets.Should().BeInRange(2, 3, $"goal={goal}");
            p.RepsMin.Should().BeInRange(8, 15, $"goal={goal}");
            p.RestSeconds.Should().BeInRange(45, 90, $"goal={goal}");
            p.TargetRpe.Should().Be("5-6");
        }
    }

    // RN-003: intermediate band: 3-4 sets, 10-20 reps, 60-180s rest, RPE contains 6-8
    [Fact]
    public void RN003_Intermediate_ParametersWithinBand()
    {
        foreach (var goal in AllGoals())
        {
            var p = ExercisePrescriptionEngine.Prescribe("intermediate", goal);
            p.Sets.Should().BeInRange(3, 4, $"goal={goal}");
            p.RepsMin.Should().BeInRange(10, 20, $"goal={goal}");
            p.RepsMax!.Value.Should().BeInRange(10, 20, $"goal={goal}");
            p.RestSeconds.Should().BeInRange(60, 180, $"goal={goal}");
        }
    }

    // RN-004: advanced band: 3-5 sets, 4-30 reps, 45-180s rest
    [Fact]
    public void RN004_Advanced_ParametersWithinBand()
    {
        foreach (var goal in AllGoals())
        {
            var p = ExercisePrescriptionEngine.Prescribe("advanced", goal);
            p.Sets.Should().BeInRange(3, 5, $"goal={goal}");
            p.RepsMin.Should().BeInRange(4, 30, $"goal={goal}");
            p.RepsMax!.Value.Should().BeInRange(4, 30, $"goal={goal}");
            p.RestSeconds.Should().BeInRange(45, 180, $"goal={goal}");
        }
    }

    // RN-005: goal adjusts reps and rest within the same level
    [Theory]
    [InlineData("intermediate")]
    [InlineData("advanced")]
    public void RN005_Goal_AffectsRepsAndRest(string level)
    {
        var muscleP    = ExercisePrescriptionEngine.Prescribe(level, "gain_muscle");
        var strengthP  = ExercisePrescriptionEngine.Prescribe(level, "gain_strength");
        var condP      = ExercisePrescriptionEngine.Prescribe(level, "improve_conditioning");

        // conditioning gets more reps than muscle/strength
        condP.RepsMin.Should().BeGreaterThan(muscleP.RepsMin, $"{level}: conditioning should have more repsMin than muscle");
        // strength gets more rest than conditioning
        strengthP.RestSeconds.Should().BeGreaterThan(condP.RestSeconds, $"{level}: strength should have more rest than conditioning");
    }

    // Goal aliases must produce identical prescriptions
    [Theory]
    [InlineData("gain_muscle", "build_muscle")]
    [InlineData("gain_strength", "more_strength")]
    [InlineData("lose_weight", "fat_loss")]
    [InlineData("improve_conditioning", "conditioning")]
    public void GoalAliases_ProduceSamePrescription(string goal1, string goal2)
    {
        foreach (var level in new[] { "sedentary", "beginner", "intermediate", "advanced" })
        {
            var p1 = ExercisePrescriptionEngine.Prescribe(level, goal1);
            var p2 = ExercisePrescriptionEngine.Prescribe(level, goal2);
            p1.Should().Be(p2, $"aliases '{goal1}' and '{goal2}' should match for level={level}");
        }
    }

    // Unknown level falls back to the most conservative (sedentary-equivalent) defaults
    [Fact]
    public void UnknownLevel_FallsBackToConservativeDefaults()
    {
        var p = ExercisePrescriptionEngine.Prescribe("elite_ninja", null);
        p.TargetRpe.Should().Be("3-5");
        p.RepsMax.Should().BeNull();
        p.Sets.Should().BeInRange(1, 2);
    }

    // Null goal resolves to default prescription for each level
    [Theory]
    [InlineData("sedentary", 1, 10, null, 60)]
    [InlineData("beginner",  2, 12, null, 60)]
    [InlineData("intermediate", 3, 10, 15, 90)]
    [InlineData("advanced",     4, 10, 15, 120)]
    public void NullGoal_ReturnsLevelDefault(
        string level, int sets, int repsMin, int? repsMax, int rest)
    {
        var p = ExercisePrescriptionEngine.Prescribe(level, null);
        p.Sets.Should().Be(sets);
        p.RepsMin.Should().Be(repsMin);
        p.RepsMax.Should().Be(repsMax);
        p.RestSeconds.Should().Be(rest);
    }

    private static IEnumerable<string?> AllGoals() =>
    [
        null,
        "gain_muscle", "build_muscle",
        "gain_strength", "more_strength",
        "lose_weight", "fat_loss",
        "improve_conditioning", "conditioning",
        "stay_active", "maintain", "health_and_consistency"
    ];
}
```

- [ ] **Step 2: Run tests and confirm they pass**

```bash
cd backend && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "ExercisePrescriptionEngineTests" --no-build
```

Expected: All tests pass (they test the code from Task 1).

---

## Task 3 — Update ExerciseDto contract

**Files:**
- Modify: `backend/src/Awaken.Contracts/Quests/QuestResponse.cs`

- [ ] **Step 1: Replace `Reps` with `RepsMin` + `RepsMax`**

Full file after change:
```csharp
namespace Awaken.Contracts.Quests;

public record QuestResponse(
    Guid Id,
    string Type,
    string Status,
    string Language,
    DateTime QuestDateUtc,
    WorkoutDto? Workout,
    long XpAwarded,
    bool IsConfirmed,
    bool IsPersonalized,
    int RegenerationsUsed,
    int RegenerationLimit);

public record WorkoutDto(
    string Title,
    string Description,
    int DurationMinutes,
    IEnumerable<ExerciseDto> Exercises);

public record ExerciseDto(
    string Name,
    string Description,
    int Sets,
    int RepsMin,
    int? RepsMax,
    int? RestSeconds,
    string? VideoUrl,
    string? TargetRpe);
```

- [ ] **Step 2: Build to find all callers that break**

```bash
cd backend && dotnet build
```

Expected: Compile errors wherever `ExerciseDto(... Reps ...)` was used — fixed in the next tasks.

---

## Task 4 — Update WorkoutGeneratorService

**Files:**
- Modify: `backend/src/Awaken.Infrastructure/Services/WorkoutGeneratorService.cs`

- [ ] **Step 1: Inject prescription engine into catalog workout generation**

Replace the `catalogWorkout` anonymous object construction (lines 79-107) and the fallback method:

Full replacement for `GenerateWorkoutJsonAsync` body — replace the section after `var selectedExercises = ...`:

```csharp
var prescription = ExercisePrescriptionEngine.Prescribe(
    profile.EffectiveExperienceLevel, profile.Goal);

var catalogWorkout = new
{
    title = "Daily Quest",
    description = "Workout generated from the approved exercise catalog.",
    durationMinutes = profile.AvailableMinutesPerWorkout,
    exercises = selectedExercises.Select(exercise => new
    {
        id = exercise.ProviderExerciseId,
        name = exercise.NamePtBr,
        description = exercise.DescriptionPtBr,
        instructions = exercise.InstructionsPtBr,
        sets = prescription.Sets,
        repsMin = prescription.RepsMin,
        repsMax = prescription.RepsMax,
        restSeconds = prescription.RestSeconds,
        targetRpe = prescription.TargetRpe,
        videoUrl = exercise.VideoUrl,
        imageUrl = exercise.ImageUrl,
        gifUrl = exercise.GifUrl,
        attributeContribution = exercise.AttributeContribution is null ? null : new
        {
            primaryAttribute = exercise.AttributeContribution.PrimaryAttribute,
            strengthXp = exercise.AttributeContribution.StrengthXp,
            agilityXp = exercise.AttributeContribution.AgilityXp,
            enduranceXp = exercise.AttributeContribution.EnduranceXp,
            vitalityXp = exercise.AttributeContribution.VitalityXp,
            focusXp = exercise.AttributeContribution.FocusXp,
            wisdomXp = exercise.AttributeContribution.WisdomXp
        }
    })
};
```

Also update the `using` block at the top to include the prescription engine namespace (it's in the same solution):

```csharp
using Awaken.Domain.Services.Quests;
```

(This using is already needed for `ExerciseSafetyFilter`, `ExerciseScoringEngine`, etc., so verify it's in the existing `using` list.)

Also update `BuildFallbackWorkoutJson` to use the prescription:

```csharp
private static string BuildFallbackWorkoutJson(
    string effectiveExperienceLevel, string? goal, int availableMinutesPerWorkout, int exerciseCount)
{
    var prescription = ExercisePrescriptionEngine.Prescribe(effectiveExperienceLevel, goal);
    var fallbackExercises = new[]
    {
        new { name = "Squat",   sets = prescription.Sets, repsMin = prescription.RepsMin, repsMax = prescription.RepsMax, restSeconds = prescription.RestSeconds, targetRpe = prescription.TargetRpe },
        new { name = "Push-up", sets = prescription.Sets, repsMin = prescription.RepsMin, repsMax = prescription.RepsMax, restSeconds = prescription.RestSeconds, targetRpe = prescription.TargetRpe },
        new { name = "Plank",   sets = prescription.Sets, repsMin = prescription.RepsMin, repsMax = prescription.RepsMax, restSeconds = prescription.RestSeconds, targetRpe = prescription.TargetRpe }
    }.Take(exerciseCount);

    var fallback = new
    {
        title = "Daily Quest",
        description = "Full body workout",
        durationMinutes = availableMinutesPerWorkout,
        exercises = fallbackExercises
    };

    return JsonSerializer.Serialize(fallback, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}
```

Update the fallback call site (where `BuildFallbackWorkoutJson` is invoked — it now needs `effectiveExperienceLevel` and `goal`):
```csharp
return new WorkoutGenerationResult(
    BuildFallbackWorkoutJson(profile.EffectiveExperienceLevel, profile.Goal, profile.AvailableMinutesPerWorkout, exerciseCount),
    IsPersonalized: false,
    GenerationMethod: "fallback_template",
    AppliedFiltersJson: appliedFiltersJson);
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build
```

Expected: `Build succeeded.`

---

## Task 5 — Update QuestResponseMapper

**Files:**
- Modify: `backend/src/Awaken.Application/Quests/Common/QuestResponseMapper.cs`

- [ ] **Step 1: Parse repsMin/repsMax with backward-compat fallback to reps**

Full replacement of `QuestResponseMapper.cs`:

```csharp
using System.Text.Json;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Services.Quests;

namespace Awaken.Application.Quests.Common;

public static class QuestResponseMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static QuestResponse ToResponse(Quest quest)
    {
        return new QuestResponse(
            Id: quest.Id,
            Type: quest.Type,
            Status: quest.Status,
            Language: quest.Language,
            QuestDateUtc: quest.QuestDateUtc,
            Workout: ParseWorkout(quest.WorkoutJson),
            XpAwarded: quest.XpAwarded,
            IsConfirmed: quest.IsConfirmed,
            IsPersonalized: quest.IsPersonalized,
            RegenerationsUsed: quest.RegenerationCount,
            RegenerationLimit: QuestRegenerationPolicy.DailyFreeLimit);
    }

    private static WorkoutDto? ParseWorkout(string? workoutJson)
    {
        if (string.IsNullOrWhiteSpace(workoutJson)) return null;

        var raw = JsonSerializer.Deserialize<RawWorkout>(workoutJson, JsonOptions);
        if (raw is null) return null;

        return new WorkoutDto(
            Title: raw.Title ?? "Daily Quest",
            Description: raw.Description ?? string.Empty,
            DurationMinutes: raw.DurationMinutes ?? 0,
            Exercises: (raw.Exercises ?? []).Select(e => new ExerciseDto(
                Name: e.Name ?? string.Empty,
                Description: e.Description ?? string.Empty,
                Sets: e.Sets ?? 0,
                RepsMin: e.RepsMin ?? e.Reps ?? 0,   // RN-007 backward compat: old JSON had 'reps'
                RepsMax: e.RepsMax,
                RestSeconds: e.RestSeconds,
                VideoUrl: e.VideoUrl,
                TargetRpe: e.TargetRpe)));
    }

    private class RawWorkout
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? DurationMinutes { get; set; }
        public List<RawExercise>? Exercises { get; set; }
    }

    private class RawExercise
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Sets { get; set; }
        public int? Reps { get; set; }       // backward compat for quests stored before US-153
        public int? RepsMin { get; set; }
        public int? RepsMax { get; set; }
        public int? RestSeconds { get; set; }
        public string? VideoUrl { get; set; }
        public string? TargetRpe { get; set; }
    }
}
```

- [ ] **Step 2: Build**

```bash
cd backend && dotnet build
```

Expected: `Build succeeded.`

---

## Task 6 — Update handler unit test fixture

**Files:**
- Modify: `backend/tests/Awaken.UnitTests/Quests/GenerateDailyQuestCommandHandlerTests.cs`

- [ ] **Step 1: Update the WorkoutJson constant to use repsMin/repsMax**

Replace the `WorkoutJson` constant (line 28–37):

```csharp
private const string WorkoutJson = """
{
  "title": "Daily Quest",
  "description": "Full body",
  "durationMinutes": 30,
  "exercises": [
    { "name": "Squat", "sets": 3, "repsMin": 10, "repsMax": 15, "restSeconds": 90, "targetRpe": "6-8" }
  ]
}
""";
```

- [ ] **Step 2: Run handler tests to confirm they still pass**

```bash
cd backend && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "GenerateDailyQuestCommandHandlerTests" --no-build
```

Expected: All tests pass.

---

## Task 7 — Add prescription assertions to integration tests

**Files:**
- Modify: `backend/tests/Awaken.IntegrationTests/QuestGenerationEndpointTests.cs`

- [ ] **Step 1: Add test for CA-001 (beginner gets fixed reps)**

Add this test to `QuestGenerationEndpointTests`:

```csharp
[Fact]
public async Task CA001_BeginnerGetsFixedReps_RepsMaxIsNull()
{
    var email = "beginner@awaken.app";
    var token = await RegisterAndGetTokenAsync(email);
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
    await StartTrialAsync();
    await CompleteOnboardingAsync(experienceLevel: "beginner", goal: "gain_muscle");

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
    await SeedApprovedExerciseDirectlyAsync(db);

    var response = await _client.PostAsync("/api/quests/daily/generate", null);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var body = await response.Content.ReadFromJsonAsync<QuestResponse>();
    body!.Workout.Should().NotBeNull();
    var exercise = body.Workout!.Exercises.First();
    exercise.RepsMax.Should().BeNull("beginners get fixed reps per RN-007");
    exercise.RepsMin.Should().BeInRange(8, 15, "beginner rep band RN-002");
    exercise.Sets.Should().BeInRange(2, 3, "beginner sets band RN-002");
}

[Fact]
public async Task CA003_IntermediateGetsRepRange_RepsMaxIsGreaterThanRepsMin()
{
    var email = "intermediate@awaken.app";
    var token = await RegisterAndGetTokenAsync(email);
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
    await StartTrialAsync();
    await CompleteOnboardingAsync(experienceLevel: "intermediate", goal: "gain_muscle");

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
    await SeedApprovedExerciseDirectlyAsync(db);

    var response = await _client.PostAsync("/api/quests/daily/generate", null);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var body = await response.Content.ReadFromJsonAsync<QuestResponse>();
    body!.Workout.Should().NotBeNull();
    var exercise = body.Workout!.Exercises.First();
    exercise.RepsMax.Should().NotBeNull("intermediate users get a rep range per CA-003/RN-007");
    exercise.RepsMax!.Should().BeGreaterThan(exercise.RepsMin);
    exercise.RepsMin.Should().BeInRange(10, 20, "intermediate rep band RN-003");
    exercise.RepsMax.Should().BeInRange(10, 20, "intermediate rep band RN-003");
}

[Fact]
public async Task CA004_SedentaryGetsFixedReps_RepsMaxIsNull()
{
    var email = "sedentary@awaken.app";
    var token = await RegisterAndGetTokenAsync(email);
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
    await StartTrialAsync();
    await CompleteOnboardingAsync(experienceLevel: "sedentary", goal: null);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
    await SeedApprovedExerciseDirectlyAsync(db);

    var response = await _client.PostAsync("/api/quests/daily/generate", null);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var body = await response.Content.ReadFromJsonAsync<QuestResponse>();
    body!.Workout.Should().NotBeNull();
    var exercise = body.Workout!.Exercises.First();
    exercise.RepsMax.Should().BeNull("sedentary users get fixed reps per CA-004/RN-007");
    exercise.RepsMin.Should().BeInRange(6, 12, "sedentary rep band RN-001");
}
```

Add the `SeedApprovedExerciseDirectlyAsync` helper (inserts one exercise via DbContext without the import API, for simpler test setup):

```csharp
private async Task SeedApprovedExerciseDirectlyAsync(AwakenDbContext db)
{
    if (await db.ExerciseCatalogs.AnyAsync()) return;

    var exercise = BuildApprovedExercise("ex001", "Squat", "strength", strengthXp: 10);
    db.ExerciseCatalogs.Add(exercise);
    await db.SaveChangesAsync();
}
```

Update the `CompleteOnboardingAsync` method signature to accept optional `experienceLevel` and `goal`:

```csharp
private async Task CompleteOnboardingAsync(
    int availableMinutesPerWorkout = 30,
    string experienceLevel = "intermediate",
    string? goal = "gain_muscle",
    string trainingDuration = "1_6_months")
{
    var payload = new
    {
        goal,
        experienceLevel,
        age = 28,
        heightCm = 175.0,
        weightKg = 82.0,
        biologicalSex = "masculino",
        trainingDuration,
        availableMinutesPerWorkout,
        bodyType = "normal",
        physicalLimitations = new[] { "no_limitations" },
        physicalPains = new[] { "no_pains" }
    };

    var response = await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", payload);
    response.EnsureSuccessStatusCode();
}
```

> **Note:** Check the existing `CompleteOnboardingAsync` signature in the file. If there is only one caller, update it and all callers. If there are many callers relying on the old defaults, add the new optional parameters preserving the old defaults as-is.

- [ ] **Step 2: Run integration tests**

```bash
cd backend && dotnet test tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj --filter "QuestGenerationEndpointTests" --no-build
```

Expected: All tests pass (old tests still pass; new tests pass).

---

## Task 8 — Flutter: update WorkoutExerciseDto

**Files:**
- Modify: `apps/mobile/lib/features/quests/data/dtos/quest_response_dto.dart`

- [ ] **Step 1: Replace `reps` with `repsMin` + `repsMax`**

Full replacement for `WorkoutExerciseDto` class:

```dart
class WorkoutExerciseDto {
  const WorkoutExerciseDto({
    required this.name,
    required this.description,
    required this.sets,
    required this.repsMin,
    this.repsMax,
    this.restSeconds,
    this.videoUrl,
    this.targetRpe,
  });

  final String name;
  final String description;
  final int sets;
  final int repsMin;
  final int? repsMax;
  final int? restSeconds;
  final String? videoUrl;
  final String? targetRpe;

  factory WorkoutExerciseDto.fromJson(Map<String, dynamic> json) {
    return WorkoutExerciseDto(
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      sets: json['sets'] as int? ?? 0,
      repsMin: json['repsMin'] as int? ?? json['reps'] as int? ?? 0,
      repsMax: json['repsMax'] as int?,
      restSeconds: json['restSeconds'] as int?,
      videoUrl: json['videoUrl'] as String?,
      targetRpe: json['targetRpe'] as String?,
    );
  }
}
```

(Keep `WorkoutDto` and `QuestResponseDto` unchanged.)

- [ ] **Step 2: Verify flutter analyze reports no errors**

```bash
cd apps/mobile && flutter analyze lib/features/quests/data/dtos/quest_response_dto.dart
```

Expected: `No issues found!`

---

## Task 9 — Flutter: update DailyQuestExercise entity

**Files:**
- Modify: `apps/mobile/lib/features/quests/domain/entities/daily_quest.dart`

- [ ] **Step 1: Replace `reps` with `repsMin`/`repsMax` and add `repsDisplay` getter**

Replace the `DailyQuestExercise` class:

```dart
class DailyQuestExercise {
  const DailyQuestExercise({
    required this.name,
    required this.description,
    required this.sets,
    required this.repsMin,
    this.repsMax,
    this.restSeconds,
    this.videoUrl,
    this.targetRpe,
  });

  final String name;
  final String description;
  final int sets;
  final int repsMin;

  /// null for sedentary/beginner (fixed reps); set for intermediate/advanced (RN-007).
  final int? repsMax;
  final int? restSeconds;
  final String? videoUrl;
  final String? targetRpe;

  /// Returns "10–15" for range prescriptions or "12" for fixed.
  /// Use with ARB keys dailyQuestExerciseRepsFixed / dailyQuestExerciseRepsRange
  /// to produce the localized display string.
  String get repsDisplay =>
      repsMax != null ? '$repsMin–$repsMax' : '$repsMin';
}
```

Keep the `DailyQuest` class and its `copyWith` method unchanged.

- [ ] **Step 2: Verify**

```bash
cd apps/mobile && flutter analyze lib/features/quests/domain/entities/daily_quest.dart
```

Expected: `No issues found!`

---

## Task 10 — Flutter: update QuestsRepositoryImpl

**Files:**
- Modify: `apps/mobile/lib/features/quests/data/repositories/quests_repository_impl.dart`

- [ ] **Step 1: Update `_toEntity` mapper**

Replace the exercise mapping inside `_toEntity`:

```dart
exercises: (workout?.exercises ?? [])
    .map((e) => DailyQuestExercise(
          name: e.name,
          description: e.description,
          sets: e.sets,
          repsMin: e.repsMin,
          repsMax: e.repsMax,
          restSeconds: e.restSeconds,
          videoUrl: e.videoUrl,
          targetRpe: e.targetRpe,
        ))
    .toList(),
```

- [ ] **Step 2: Verify**

```bash
cd apps/mobile && flutter analyze lib/features/quests/data/repositories/quests_repository_impl.dart
```

Expected: `No issues found!`

---

## Task 11 — Flutter: update DailyQuestNotification widget

**Files:**
- Modify: `apps/mobile/lib/features/quests/presentation/widgets/daily_quest_notification.dart`

- [ ] **Step 1: Update `_ExerciseRow` to use `repsMin` and show range for int/adv**

Replace the `Text` widget in `_ExerciseRow.build` that calls `dailyQuestNotificationExerciseMeta`:

```dart
Text(
  exercise.repsMax != null
      ? l10n.dailyQuestNotificationExerciseMetaRange(
          exercise.sets, exercise.repsMin, exercise.repsMax!)
      : l10n.dailyQuestNotificationExerciseMeta(
          exercise.sets, exercise.repsMin),
  style: AwakenTypography.stat,
),
```

- [ ] **Step 2: Verify (will fail until ARB keys are regenerated in Task 12+13)**

After Task 12+13, run:
```bash
cd apps/mobile && flutter analyze lib/features/quests/presentation/widgets/daily_quest_notification.dart
```

Expected: `No issues found!`

---

## Task 12 — Update ARB files (all 4 languages)

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`
- Modify: `apps/mobile/lib/l10n/app_fr.arb`

- [ ] **Step 1: Update `dailyQuestNotificationExerciseMeta` key in app_pt.arb**

Find and replace the key+annotation (placeholder name changes from `reps` to `repsMin`):

```json
"dailyQuestNotificationExerciseMeta": "{sets}x{repsMin}",
"@dailyQuestNotificationExerciseMeta": { "description": "Séries x repetições mínimas do exercício na notificação de quest diária", "placeholders": { "sets": { "type": "num", "format": "decimalPattern" }, "repsMin": { "type": "num", "format": "decimalPattern" } } },
```

- [ ] **Step 2: Add new keys to app_pt.arb** (after `dailyQuestNotificationExerciseMeta`):

```json
"dailyQuestNotificationExerciseMetaRange": "{sets}x{repsMin}–{repsMax}",
"@dailyQuestNotificationExerciseMetaRange": { "description": "Séries x intervalo de repetições do exercício na notificação de quest diária (intermediário/avançado)", "placeholders": { "sets": { "type": "num", "format": "decimalPattern" }, "repsMin": { "type": "num", "format": "decimalPattern" }, "repsMax": { "type": "num", "format": "decimalPattern" } } },
"dailyQuestExerciseRepsFixed": "{reps} reps",
"@dailyQuestExerciseRepsFixed": { "description": "Exibição de repetições fixas (sedentário/iniciante)", "placeholders": { "reps": { "type": "int" } } },
"dailyQuestExerciseRepsRange": "{repsMin}–{repsMax} reps",
"@dailyQuestExerciseRepsRange": { "description": "Exibição de intervalo de repetições (intermediário/avançado)", "placeholders": { "repsMin": { "type": "int" }, "repsMax": { "type": "int" } } }
```

- [ ] **Step 3: Apply same changes to app_en.arb**

```json
"dailyQuestNotificationExerciseMeta": "{sets}x{repsMin}",
"@dailyQuestNotificationExerciseMeta": { "description": "Sets x minimum reps of the exercise in the daily quest notification", "placeholders": { "sets": { "type": "num", "format": "decimalPattern" }, "repsMin": { "type": "num", "format": "decimalPattern" } } },
"dailyQuestNotificationExerciseMetaRange": "{sets}x{repsMin}–{repsMax}",
"@dailyQuestNotificationExerciseMetaRange": { "description": "Sets x rep range of the exercise in the daily quest notification (intermediate/advanced)", "placeholders": { "sets": { "type": "num", "format": "decimalPattern" }, "repsMin": { "type": "num", "format": "decimalPattern" }, "repsMax": { "type": "num", "format": "decimalPattern" } } },
"dailyQuestExerciseRepsFixed": "{reps} reps",
"@dailyQuestExerciseRepsFixed": { "description": "Fixed rep count display (sedentary/beginner)", "placeholders": { "reps": { "type": "int" } } },
"dailyQuestExerciseRepsRange": "{repsMin}–{repsMax} reps",
"@dailyQuestExerciseRepsRange": { "description": "Rep range display (intermediate/advanced)", "placeholders": { "repsMin": { "type": "int" }, "repsMax": { "type": "int" } } }
```

- [ ] **Step 4: Apply same changes to app_es.arb**

```json
"dailyQuestNotificationExerciseMeta": "{sets}x{repsMin}",
"@dailyQuestNotificationExerciseMeta": { "description": "Series x repeticiones mínimas del ejercicio en la notificación de quest diaria", "placeholders": { "sets": { "type": "num", "format": "decimalPattern" }, "repsMin": { "type": "num", "format": "decimalPattern" } } },
"dailyQuestNotificationExerciseMetaRange": "{sets}x{repsMin}–{repsMax}",
"@dailyQuestNotificationExerciseMetaRange": { "description": "Series x intervalo de repeticiones del ejercicio en la notificación de quest diaria (intermedio/avanzado)", "placeholders": { "sets": { "type": "num", "format": "decimalPattern" }, "repsMin": { "type": "num", "format": "decimalPattern" }, "repsMax": { "type": "num", "format": "decimalPattern" } } },
"dailyQuestExerciseRepsFixed": "{reps} reps",
"@dailyQuestExerciseRepsFixed": { "description": "Visualización de repeticiones fijas (sedentario/principiante)", "placeholders": { "reps": { "type": "int" } } },
"dailyQuestExerciseRepsRange": "{repsMin}–{repsMax} reps",
"@dailyQuestExerciseRepsRange": { "description": "Visualización de intervalo de repeticiones (intermedio/avanzado)", "placeholders": { "repsMin": { "type": "int" }, "repsMax": { "type": "int" } } }
```

- [ ] **Step 5: Apply same changes to app_fr.arb**

```json
"dailyQuestNotificationExerciseMeta": "{sets}x{repsMin}",
"@dailyQuestNotificationExerciseMeta": { "description": "Séries x répétitions minimales de l'exercice dans la notification de quête quotidienne", "placeholders": { "sets": { "type": "num", "format": "decimalPattern" }, "repsMin": { "type": "num", "format": "decimalPattern" } } },
"dailyQuestNotificationExerciseMetaRange": "{sets}x{repsMin}–{repsMax}",
"@dailyQuestNotificationExerciseMetaRange": { "description": "Séries x intervalle de répétitions de l'exercice dans la notification de quête quotidienne (intermédiaire/avancé)", "placeholders": { "sets": { "type": "num", "format": "decimalPattern" }, "repsMin": { "type": "num", "format": "decimalPattern" }, "repsMax": { "type": "num", "format": "decimalPattern" } } },
"dailyQuestExerciseRepsFixed": "{reps} reps",
"@dailyQuestExerciseRepsFixed": { "description": "Affichage des répétitions fixes (sédentaire/débutant)", "placeholders": { "reps": { "type": "int" } } },
"dailyQuestExerciseRepsRange": "{repsMin}–{repsMax} reps",
"@dailyQuestExerciseRepsRange": { "description": "Affichage de l'intervalle de répétitions (intermédiaire/avancé)", "placeholders": { "repsMin": { "type": "int" }, "repsMax": { "type": "int" } } }
```

---

## Task 13 — Regenerate l10n and verify

- [ ] **Step 1: Run flutter gen-l10n**

```bash
cd apps/mobile && flutter gen-l10n
```

Expected: No errors. Regenerates `lib/l10n/app_localizations*.dart`.

- [ ] **Step 2: Run flutter analyze on the whole project**

```bash
cd apps/mobile && flutter analyze
```

Expected: `No issues found!` (the notification widget now calls the correct generated methods)

---

## Task 14 — Update Flutter tests

**Files:**
- Modify: `apps/mobile/test/features/quests/data/datasources/quests_remote_data_source_test.dart`
- Modify: `apps/mobile/test/features/quests/data/repositories/quests_repository_impl_test.dart`

- [ ] **Step 1: Update `_questBody` fixture in data source test**

Replace the exercise object in `_questBody` (in both test files — they have the same fixture):

```dart
{'name': 'Push-up', 'description': '', 'sets': 3, 'repsMin': 10, 'repsMax': 15, 'restSeconds': 90, 'targetRpe': '6-8'},
```

- [ ] **Step 2: Update existing test assertions in repository impl test**

In `quests_repository_impl_test.dart`, add `repsMin` assertion to the existing exercise test:

```dart
expect(quest.exercises.single.name, 'Squat');
expect(quest.exercises.single.sets, 3);
expect(quest.exercises.single.repsMin, 10);
expect(quest.exercises.single.repsMax, 15);
```

- [ ] **Step 3: Add CA-003 test (intermediate gets range) to repository impl test**

```dart
test('CA-003 intermediate exercise gets repsMin and repsMax set', () async {
  final body = _questBody(
    workout: {
      'title': 'Daily Quest',
      'description': 'Full body',
      'durationMinutes': 30,
      'exercises': [
        {
          'name': 'Squat',
          'description': '',
          'sets': 4,
          'repsMin': 10,
          'repsMax': 15,
          'restSeconds': 90,
          'targetRpe': '6-8',
        },
      ],
    },
  );
  final repository = _buildRepository(200, body);

  final quest = await repository.generateDailyQuest();

  final exercise = quest.exercises.single;
  expect(exercise.repsMin, 10);
  expect(exercise.repsMax, 15);
  expect(exercise.repsDisplay, '10–15');
  expect(exercise.targetRpe, '6-8');
});

test('CA-004 sedentary/beginner exercise gets null repsMax', () async {
  final body = _questBody(
    workout: {
      'title': 'Daily Quest',
      'description': 'Full body',
      'durationMinutes': 30,
      'exercises': [
        {
          'name': 'Push-up',
          'description': '',
          'sets': 2,
          'repsMin': 12,
          'restSeconds': 60,
          'targetRpe': '5-6',
        },
      ],
    },
  );
  final repository = _buildRepository(200, body);

  final quest = await repository.generateDailyQuest();

  final exercise = quest.exercises.single;
  expect(exercise.repsMin, 12);
  expect(exercise.repsMax, isNull);
  expect(exercise.repsDisplay, '12');
});

test('backward compat: old JSON with reps field maps to repsMin', () async {
  final body = _questBody(
    workout: {
      'title': 'Daily Quest',
      'description': 'Full body',
      'durationMinutes': 30,
      'exercises': [
        {'name': 'Squat', 'description': '', 'sets': 3, 'reps': 12, 'restSeconds': 60},
      ],
    },
  );
  final repository = _buildRepository(200, body);

  final quest = await repository.generateDailyQuest();

  expect(quest.exercises.single.repsMin, 12);
  expect(quest.exercises.single.repsMax, isNull);
});
```

- [ ] **Step 4: Run Flutter tests**

```bash
cd apps/mobile && flutter test test/features/quests/
```

Expected: All tests pass.

---

## Task 15 — Final verification

- [ ] **Step 1: Run all backend tests**

```bash
cd backend && dotnet test
```

Expected: All tests pass. Note any new failures and fix before proceeding.

- [ ] **Step 2: Run Flutter full test suite**

```bash
cd apps/mobile && flutter test
```

Expected: All tests pass.

- [ ] **Step 3: Run flutter analyze (full project)**

```bash
cd apps/mobile && flutter analyze
```

Expected: `No issues found!`

- [ ] **Step 4: Verify l10n coverage — all 4 languages have the new keys**

```bash
cd apps/mobile && grep -l "dailyQuestExerciseRepsRange" lib/l10n/app_*.arb
```

Expected: All 4 ARB files listed (`app_pt.arb`, `app_en.arb`, `app_es.arb`, `app_fr.arb`).

---

## Self-Review Checklist

| Spec requirement | Covered by task(s) |
|---|---|
| RN-001 Sedentário: 1–2 sets, 6–12 reps fixed, 45–90s rest, RPE 3–5 | T1 (engine) + T2 (tests) |
| RN-002 Iniciante: 2–3 sets, 8–15 reps fixed, 45–90s rest, RPE 5–6 | T1 + T2 |
| RN-003 Intermediário: 3–4 sets, 10–20 range, 60–180s rest, RPE 6–8 | T1 + T2 |
| RN-004 Avançado: 3–5 sets, 4–30 range, rest per goal, RPE 6–9 | T1 + T2 |
| RN-005 Goal adjusts reps/rest/emphasis | T1 (tables) + T2 (alias + goal diff tests) |
| RN-006 Prescription never contradicts safety | ExerciseSafetyFilter already runs before prescription; no new code needed |
| RN-007 sed/init: repsMax=null; int/adv: [repsMin, repsMax] | T1 + T2 + T8 + T9 + T14 |
| CA-001 Beginner → correct bands | T7 (integration) + T2 (unit) |
| CA-002 Micro quest + conflict | Existing flow unchanged (micro quest reduces exercises, prescription still applies) |
| CA-003 Intermediate → range format in response | T7 (integration) + T14 (Flutter) |
| CA-004 Sedentary → fixed reps, repsMax=null | T7 (integration) + T14 (Flutter) |
| Frontend: "X reps" vs "X–Y reps" display | T11 (notification widget) + T12 (ARB keys) + T9 (repsDisplay getter) |
| Backward compat: old quest JSON with `reps` | T5 (mapper fallback) + T14 (Flutter fallback + test) |
| Analytics event `daily_quest_generated` | Already fired by existing handler — no change needed |
| 4-language coverage | T12 (all 4 ARB files updated) |
