using Awaken.Application.Quests.Common;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

/// US-065: testes da fórmula de XP por esforço, dificuldade e conclusão.
public class XpCalculationServiceTests
{
    // CA-001: conclusão completa concede XP cheio.
    [Fact]
    public void FullCompletion_ReturnsFullXpReward()
    {
        var xp = XpCalculationService.CalculateExerciseXp(
            xpReward: 24, totalSets: 3, setsCompleted: 3, strongPainReported: false);

        xp.Should().Be(24);
    }

    // CA-001: conclusão de 50% concede XP proporcional.
    [Fact]
    public void HalfCompletion_ReturnsHalfXp()
    {
        var xp = XpCalculationService.CalculateExerciseXp(
            xpReward: 24, totalSets: 4, setsCompleted: 2, strongPainReported: false);

        xp.Should().Be(12);
    }

    // RN-002: conclusão parcial de 1/3 concede XP proporcional.
    [Fact]
    public void OneOfThreeSets_ReturnsRoundedProportional()
    {
        var xp = XpCalculationService.CalculateExerciseXp(
            xpReward: 30, totalSets: 3, setsCompleted: 1, strongPainReported: false);

        xp.Should().Be(10);
    }

    // CA-002 / RN-003: dor forte não aumenta XP além do proporcional puro.
    [Fact]
    public void StrongPain_NeverExceedsNoPainXp()
    {
        var withoutPain = XpCalculationService.CalculateExerciseXp(
            xpReward: 20, totalSets: 3, setsCompleted: 3, strongPainReported: false);

        var withPain = XpCalculationService.CalculateExerciseXp(
            xpReward: 20, totalSets: 3, setsCompleted: 3, strongPainReported: true);

        withPain.Should().BeLessThanOrEqualTo(withoutPain);
    }

    // CA-002: dor forte em conclusão completa → mesmo XP proporcional.
    [Fact]
    public void StrongPainFullCompletion_ReturnsSameAsNoPain()
    {
        var xp = XpCalculationService.CalculateExerciseXp(
            xpReward: 20, totalSets: 3, setsCompleted: 3, strongPainReported: true);

        xp.Should().Be(20);
    }

    // Margem de arredondamento: 1/3 de 10 → arredondado para 3.
    [Fact]
    public void Rounding_RoundsToNearestLong()
    {
        var xp = XpCalculationService.CalculateExerciseXp(
            xpReward: 10, totalSets: 3, setsCompleted: 1, strongPainReported: false);

        xp.Should().Be(3);
    }

    // Setsompleted excedendo total → clampado ao total (sem super-recompensa).
    [Fact]
    public void SetsCompletedExceedsTotal_ClampsToTotal()
    {
        var xp = XpCalculationService.CalculateExerciseXp(
            xpReward: 24, totalSets: 3, setsCompleted: 5, strongPainReported: false);

        xp.Should().Be(24);
    }

    // Edge case: setsCompleted = 0 → XP = 0.
    [Fact]
    public void ZeroSetsCompleted_ReturnsZero()
    {
        var xp = XpCalculationService.CalculateExerciseXp(
            xpReward: 24, totalSets: 3, setsCompleted: 0, strongPainReported: false);

        xp.Should().Be(0);
    }

    // Edge case: totalSets = 0 → XP = 0 (não divide por zero).
    [Fact]
    public void ZeroTotalSets_ReturnsZero()
    {
        var xp = XpCalculationService.CalculateExerciseXp(
            xpReward: 24, totalSets: 0, setsCompleted: 1, strongPainReported: false);

        xp.Should().Be(0);
    }

    // AverageQuestXp: retorna média correta.
    [Fact]
    public void AverageQuestXp_ReturnsCorrectAverage()
    {
        var avg = XpCalculationService.AverageQuestXp([24, 12, 18]);

        avg.Should().Be(18);
    }

    // AverageQuestXp: lista vazia → 0.
    [Fact]
    public void AverageQuestXp_EmptyList_ReturnsZero()
    {
        var avg = XpCalculationService.AverageQuestXp([]);

        avg.Should().Be(0);
    }
}
