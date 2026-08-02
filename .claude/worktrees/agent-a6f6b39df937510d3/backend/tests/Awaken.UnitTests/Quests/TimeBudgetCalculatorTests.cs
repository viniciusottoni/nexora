using Awaken.Domain.Services.Quests;
using FluentAssertions;
using Xunit;

namespace Awaken.UnitTests.Quests;

public class TimeBudgetCalculatorTests
{
    [Fact]
    public void ExerciseTimeCostSeconds_SumsTransitionExecutionAndRestBetweenSets()
    {
        // 3 séries de 10 reps a 3s/rep = 30s/série; descanso 60s ENTRE séries (2×, não 3×); transição 45s.
        var cost = TimeBudgetCalculator.ExerciseTimeCostSeconds(sets: 3, reps: 10, plannedDurationSeconds: null, restSeconds: 60);
        cost.Should().Be(45 + 3 * 30 + 2 * 60);
    }

    [Fact]
    public void ExerciseTimeCostSeconds_UsesPlannedDurationForTimedExercises()
    {
        var cost = TimeBudgetCalculator.ExerciseTimeCostSeconds(sets: 3, reps: 0, plannedDurationSeconds: 45, restSeconds: 30);
        cost.Should().Be(45 + 3 * 45 + 2 * 30);
    }

    [Fact]
    public void EstimateQuestDurationSeconds_IncludesWarmupAndCooldown()
    {
        var total = TimeBudgetCalculator.EstimateQuestDurationSeconds([100, 200], warmupSeconds: 300, cooldownSeconds: 120);
        total.Should().Be(300 + 100 + 200 + 120);
    }

    [Fact]
    public void Resolve_NeverExceedsAvailableTime()
    {
        var items = Enumerable.Range(0, 8)
            .Select(i => new TimeBudgetItem($"ex{i}", Sets: 4, Reps: 12, PlannedDurationSeconds: null, RestSeconds: 90, PriorityScore: 1.0 - i * 0.01))
            .ToList();
        var request = new TimeBudgetRequest(items, ExtraCandidates: [], EffectiveExperienceLevel: "intermediate", Goal: "gain_muscle", AvailableMinutesPerWorkout: 20);

        var result = TimeBudgetCalculator.Resolve(request);

        result.EstimatedDurationSeconds.Should().BeLessThanOrEqualTo(result.TimeBudgetSeconds);
    }

    [Fact]
    public void Resolve_StrengthGoalReducesExercisesAndSetsBeforeTouchingRest()
    {
        var items = Enumerable.Range(0, 5)
            .Select(i => new TimeBudgetItem($"ex{i}", Sets: 4, Reps: 8, PlannedDurationSeconds: null, RestSeconds: 150, PriorityScore: 1.0 - i * 0.1))
            .ToList();
        var request = new TimeBudgetRequest(items, ExtraCandidates: [], EffectiveExperienceLevel: "advanced", Goal: "gain_strength", AvailableMinutesPerWorkout: 20);

        var result = TimeBudgetCalculator.Resolve(request);

        result.TimeAdjustmentApplied.Should().BeOneOf("reduced_exercises", "reduced_sets", "micro_quest");
        result.Items.Should().OnlyContain(i => i.RestSeconds == 150); // descanso do objetivo preservado (RN-004)
    }

    [Fact]
    public void Resolve_ConditioningGoalAppliesDensityWhenStillOverBudgetAfterVolumeCuts()
    {
        // Sets=2 sobrevive ao piso de "reduzir séries" (que para em 2 antes de tentar
        // densidade, RN-004) - só assim o descanso ENTRE séries ainda pesa no custo
        // e a densidade (RestSeconds/2) tem efeito real de reduzir o total.
        var items = new List<TimeBudgetItem> { new("ex0", Sets: 2, Reps: 12, PlannedDurationSeconds: null, RestSeconds: 300, PriorityScore: 1.0) };
        var request = new TimeBudgetRequest(items, ExtraCandidates: [], EffectiveExperienceLevel: "advanced", Goal: "conditioning", AvailableMinutesPerWorkout: 17, MinExerciseCount: 1);

        var result = TimeBudgetCalculator.Resolve(request);

        result.DensityApplied.Should().BeTrue();
        result.Items.Single().RestSeconds.Should().BeLessThan(300);
    }

    [Fact]
    public void Resolve_ShortTimeUsesMicroQuestFormat()
    {
        var items = new List<TimeBudgetItem> { new("ex0", Sets: 3, Reps: 12, PlannedDurationSeconds: null, RestSeconds: 60, PriorityScore: 1.0) };
        var request = new TimeBudgetRequest(items, ExtraCandidates: [], EffectiveExperienceLevel: "beginner", Goal: "gain_muscle", AvailableMinutesPerWorkout: 10);

        var result = TimeBudgetCalculator.Resolve(request);

        result.IsMicroQuest.Should().BeTrue();
        result.WarmupSeconds.Should().Be(WorkoutTimeModel.MicroQuestWarmupSeconds);
        result.CooldownSeconds.Should().Be(0);
    }

    [Fact]
    public void Resolve_AddsSetsThenExtraExerciseWhenBelowMinUtilization()
    {
        var items = new List<TimeBudgetItem> { new("ex0", Sets: 2, Reps: 10, PlannedDurationSeconds: null, RestSeconds: 30, PriorityScore: 1.0) };
        var extra = new List<TimeBudgetItem> { new("ex1", Sets: 2, Reps: 10, PlannedDurationSeconds: null, RestSeconds: 30, PriorityScore: 0.9) };
        var request = new TimeBudgetRequest(items, extra, EffectiveExperienceLevel: "intermediate", Goal: "gain_muscle", AvailableMinutesPerWorkout: 18);

        var result = TimeBudgetCalculator.Resolve(request);

        result.TimeAdjustmentApplied.Should().Be("added_volume");
        result.Utilization.Should().BeGreaterThanOrEqualTo((double)WorkoutTimeModel.MinUtilization - 0.05);
    }

    [Fact]
    public void Resolve_IsDeterministic()
    {
        var items = new List<TimeBudgetItem> { new("ex0", Sets: 4, Reps: 10, PlannedDurationSeconds: null, RestSeconds: 90, PriorityScore: 1.0) };
        var request = new TimeBudgetRequest(items, ExtraCandidates: [], EffectiveExperienceLevel: "intermediate", Goal: "gain_muscle", AvailableMinutesPerWorkout: 12);

        var first = TimeBudgetCalculator.Resolve(request);
        var second = TimeBudgetCalculator.Resolve(request);

        first.Should().BeEquivalentTo(second);
    }
}
