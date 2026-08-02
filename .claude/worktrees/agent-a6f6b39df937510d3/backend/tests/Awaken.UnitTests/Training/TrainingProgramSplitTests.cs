using Awaken.Domain.Entities.Training;
using FluentAssertions;

namespace Awaken.UnitTests.Training;

// US-237: TrainingProgramSplit é a fonte de verdade determinística sobre o
// que cada dia (letra) de cada programa treina (grupos/padrões-alvo).
public class TrainingProgramSplitTests
{
    private static TrainingSplitDaySeed ValidDay(
        string dayKey = "A",
        string role = "push",
        string labelI18nKey = "programDayPush",
        IReadOnlyList<string>? targetMuscleGroups = null,
        IReadOnlyList<string>? secondaryMuscleGroups = null,
        IReadOnlyList<string>? targetMovementPatterns = null,
        bool allowsCoreFinisher = true,
        int minExercises = 4,
        int maxExercises = 6) => new(
            dayKey,
            labelI18nKey,
            role,
            targetMuscleGroups ?? [MuscleGroups.Chest],
            secondaryMuscleGroups ?? [],
            targetMovementPatterns ?? [MovementPatterns.HorizontalPush],
            allowsCoreFinisher,
            minExercises,
            maxExercises);

    // CA-001 — os 5 programas clássicos geram a quantidade correta de dias.
    [Theory]
    [InlineData(TrainingProgramKeys.FullBody, 1)]
    [InlineData(TrainingProgramKeys.Ab, 2)]
    [InlineData(TrainingProgramKeys.Abc, 3)]
    [InlineData(TrainingProgramKeys.Abcd, 4)]
    [InlineData(TrainingProgramKeys.Abcde, 5)]
    public void Create_ResultsInExpectedDayCount_ForClassicPrograms(string programKey, int dayCount)
    {
        var days = Enumerable.Range(1, dayCount)
            .Select(i => ValidDay(dayKey: $"D{i}"))
            .ToList();

        var split = TrainingProgramSplit.Create(programKey, "v1", days);

        split.DayCount.Should().Be(dayCount);
        split.Days.Should().HaveCount(dayCount);
    }

    [Fact]
    public void Create_Throws_WhenClassicProgramHasWrongDayCount()
    {
        var act = () => TrainingProgramSplit.Create(
            TrainingProgramKeys.Ab,
            "v1",
            [ValidDay(dayKey: "A")]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exatamente 2*");
    }

    // CA-002 — AB integra pernas nos dois dias: dia A com quadríceps (push),
    // dia B com posterior/glúteo (pull/hinge). Ambos devem passar sem exceção.
    [Fact]
    public void Create_AllowsAb_WithLegsIntegratedAcrossBothDays()
    {
        var dayA = ValidDay(
            dayKey: "A",
            role: "push",
            labelI18nKey: "programDayPush",
            targetMuscleGroups: [MuscleGroups.Chest, MuscleGroups.Shoulders, MuscleGroups.Triceps, MuscleGroups.Quadriceps, MuscleGroups.Calves],
            targetMovementPatterns: [MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush, MovementPatterns.Squat, MovementPatterns.Lunge, MovementPatterns.CoreFlexion]);

        var dayB = ValidDay(
            dayKey: "B",
            role: "pull",
            labelI18nKey: "programDayPull",
            targetMuscleGroups: [MuscleGroups.Back, MuscleGroups.Biceps, MuscleGroups.RearDelts, MuscleGroups.Hamstrings, MuscleGroups.Glutes],
            targetMovementPatterns: [MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull, MovementPatterns.Hinge, MovementPatterns.CoreFlexion]);

        var act = () => TrainingProgramSplit.Create(TrainingProgramKeys.Ab, "v1", [dayA, dayB]);

        act.Should().NotThrow();
    }

    // CA-003 (RN-005) — grupo muscular fora do enum bloqueia o seed.
    [Fact]
    public void Create_Throws_WhenMuscleGroupIsInvalid()
    {
        var day = ValidDay(targetMuscleGroups: ["bogus_muscle"]);

        var act = () => TrainingProgramSplit.Create(TrainingProgramKeys.Abc, "v1", [day]);

        act.Should().Throw<InvalidOperationException>();
    }

    // RN-005 — padrão de movimento fora do enum também bloqueia o seed.
    [Fact]
    public void Create_Throws_WhenMovementPatternIsInvalid()
    {
        var day = ValidDay(targetMovementPatterns: ["bogus_pattern"]);

        var act = () => TrainingProgramSplit.Create(TrainingProgramKeys.Abc, "v1", [day]);

        act.Should().Throw<InvalidOperationException>();
    }

    // RN-002 — dia sem grupo muscular-alvo bloqueia o seed.
    [Fact]
    public void Create_Throws_WhenDayHasNoTargetMuscleGroups()
    {
        var day = ValidDay(targetMuscleGroups: []);

        var act = () => TrainingProgramSplit.Create(TrainingProgramKeys.Abc, "v1", [day]);

        act.Should().Throw<InvalidOperationException>();
    }

    // RN-002 — dia sem padrão de movimento-alvo bloqueia o seed.
    [Fact]
    public void Create_Throws_WhenDayHasNoTargetMovementPatterns()
    {
        var day = ValidDay(targetMovementPatterns: []);

        var act = () => TrainingProgramSplit.Create(TrainingProgramKeys.Abc, "v1", [day]);

        act.Should().Throw<InvalidOperationException>();
    }

    // RN-002 — split sem nenhum dia bloqueia o seed.
    [Fact]
    public void Create_Throws_WhenNoDaysProvided()
    {
        var act = () => TrainingProgramSplit.Create(TrainingProgramKeys.FullBody, "v1", []);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_AssignsSequentialDayIndex_StartingAtOne()
    {
        var days = new[] { ValidDay(dayKey: "A"), ValidDay(dayKey: "B"), ValidDay(dayKey: "C") };

        var split = TrainingProgramSplit.Create(TrainingProgramKeys.Abc, "v1", days);

        split.Days.Select(d => d.DayIndex).Should().Equal(1, 2, 3);
        split.Days.Select(d => d.DayKey).Should().Equal("A", "B", "C");
    }
}
