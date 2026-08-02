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

    // RN-003: intermediate band: 3-4 sets, 10-20 reps, 60-180s rest
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
        var muscleP   = ExercisePrescriptionEngine.Prescribe(level, "gain_muscle");
        var strengthP = ExercisePrescriptionEngine.Prescribe(level, "gain_strength");
        var condP     = ExercisePrescriptionEngine.Prescribe(level, "improve_conditioning");

        condP.RepsMin.Should().BeGreaterThan(muscleP.RepsMin, $"{level}: conditioning should have more repsMin than muscle");
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

    // Unknown level falls back to the most conservative (sedentary) defaults
    [Fact]
    public void UnknownLevel_FallsBackToConservativeDefaults()
    {
        var p = ExercisePrescriptionEngine.Prescribe("elite_ninja", null);
        p.TargetRpe.Should().Be("3-5");
        p.RepsMax.Should().BeNull();
        p.Sets.Should().BeInRange(1, 2);
    }

    // Null goal resolves to default prescription per level
    [Theory]
    [InlineData("sedentary",    1, 10, null, 60)]
    [InlineData("beginner",     2, 12, null, 60)]
    [InlineData("intermediate", 3, 10,   15, 90)]
    [InlineData("advanced",     4, 10,   15, 120)]
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
