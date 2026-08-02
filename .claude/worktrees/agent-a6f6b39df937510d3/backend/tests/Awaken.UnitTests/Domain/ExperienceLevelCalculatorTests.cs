using Awaken.Domain.Services.Progression;
using FluentAssertions;

namespace Awaken.UnitTests.Domain;

public class ExperienceLevelCalculatorTests
{
    [Fact]
    public void CA001_StrongConflict_AdvancedDeclaredButNeverTrained_ResultsInSedentary()
    {
        var result = ExperienceLevelCalculator.CalculateEffectiveLevel("advanced", "does_not_train");

        result.Should().Be("sedentary");
    }

    [Theory]
    [InlineData("sedentary", "does_not_train", "sedentary")]
    [InlineData("beginner", "does_not_train", "sedentary")] // RN-001: "não treino" trata como sedentário, mesmo se declarado iniciante.
    [InlineData("beginner", "less_than_1_month", "beginner")] // RN-001: "menos de 1 mês" como iniciante absoluto.
    [InlineData("intermediate", "1_6_months", "beginner")] // RN-002: "1 a 6 meses" iniciante em consolidação.
    [InlineData("intermediate", "6_12_months", "intermediate")] // RN-002: "6 a 12 meses" intermediário leve.
    [InlineData("advanced", "more_than_1_year", "intermediate")] // RN-003: "mais de 1 ano" pode ser intermediário.
    [InlineData("advanced", "more_than_3_years", "advanced")] // RN-003: "mais de 3 anos" pode liberar avançado, se confirmado.
    public void RN001ToRN003_DerivesLevelFromExperienceAndDuration(
        string experienceLevel, string trainingDuration, string expected)
    {
        var result = ExperienceLevelCalculator.CalculateEffectiveLevel(experienceLevel, trainingDuration);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("advanced", "1_6_months", "beginner")]
    [InlineData("intermediate", "less_than_1_month", "beginner")]
    [InlineData("advanced", "6_12_months", "intermediate")]
    public void RN004_ConflictBetweenDeclaredAndImpliedLevel_AlwaysPicksTheSaferOne(
        string experienceLevel, string trainingDuration, string expected)
    {
        var result = ExperienceLevelCalculator.CalculateEffectiveLevel(experienceLevel, trainingDuration);

        result.Should().Be(expected);
    }

    [Fact]
    public void DeclaredLevelBelowDurationImpliedLevel_KeepsDeclaredLevel()
    {
        // Usuário declara sedentário mas treina há mais de 3 anos: mantém o mais conservador (declarado).
        var result = ExperienceLevelCalculator.CalculateEffectiveLevel("sedentary", "more_than_3_years");

        result.Should().Be("sedentary");
    }

    [Theory]
    [InlineData(null, "does_not_train")]
    [InlineData("advanced", null)]
    [InlineData(null, null)]
    [InlineData("unknown_level", "unknown_duration")]
    public void UnknownOrMissingValues_FallBackToSedentary(string? experienceLevel, string? trainingDuration)
    {
        var result = ExperienceLevelCalculator.CalculateEffectiveLevel(experienceLevel, trainingDuration);

        result.Should().Be("sedentary");
    }
}
