using Awaken.Domain.Entities.Quests;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

public class QuestExerciseTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 23, 10, 0, 0, DateTimeKind.Utc);

    private static QuestExercise BuildExercise() => QuestExercise.Create(
        Guid.NewGuid(),
        order: 1,
        seed: new QuestExerciseSeed(
            Name: "Squat",
            ExerciseCatalogProviderId: "ex-1",
            Sets: 3,
            RepsMin: 8,
            RepsMax: 12,
            RestSeconds: 60,
            TargetRpe: "8",
            VideoUrl: null,
            XpReward: 24,
            StrengthXp: 2,
            AgilityXp: 0,
            EnduranceXp: 0,
            VitalityXp: 0,
            FocusXp: 0,
            WisdomXp: 1));

    // RN-002/RN-006 US-058/US-065: 1a chamada conclui com XP calculado.
    [Fact]
    public void MarkCompleted_FirstCall_ReturnsTrue_AndSetsCompletionFields()
    {
        var exercise = BuildExercise();

        var awarded = exercise.MarkCompleted(UtcNow, calculatedXp: 24);

        awarded.Should().BeTrue();
        exercise.Status.Should().Be("completed");
        exercise.CompletedAtUtc.Should().Be(UtcNow);
        exercise.XpEarned.Should().Be(24);
    }

    // US-065: XP proporcional (partial) é armazenado corretamente.
    [Fact]
    public void MarkCompleted_PartialXp_StoresCalculatedXp()
    {
        var exercise = BuildExercise();

        exercise.MarkCompleted(UtcNow, calculatedXp: 16);

        exercise.XpEarned.Should().Be(16);
    }

    // RN-002/RN-006 US-058: idempotente - 2a chamada nao concede de novo nem sobrescreve dados.
    [Fact]
    public void MarkCompleted_SecondCall_ReturnsFalse_AndDoesNotOverwriteCompletionFields()
    {
        var exercise = BuildExercise();
        exercise.MarkCompleted(UtcNow, calculatedXp: 24);

        var awardedAgain = exercise.MarkCompleted(UtcNow.AddHours(1), calculatedXp: 24);

        awardedAgain.Should().BeFalse();
        exercise.CompletedAtUtc.Should().Be(UtcNow);
        exercise.XpEarned.Should().Be(24);
    }
}
