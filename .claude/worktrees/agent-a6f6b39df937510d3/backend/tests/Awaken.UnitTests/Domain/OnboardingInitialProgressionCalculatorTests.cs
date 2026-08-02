using Awaken.Domain.Services.Progression;
using FluentAssertions;

namespace Awaken.UnitTests.Domain;

public class OnboardingInitialProgressionCalculatorTests
{
    [Fact]
    public void RN001_RankScoreIsSumOfDerivedAttributes()
    {
        var result = OnboardingInitialProgressionCalculator.Calculate("beginner", "1_6_months");

        var sum = result.Attributes.Strength + result.Attributes.Agility + result.Attributes.Endurance
            + result.Attributes.Vitality + result.Attributes.Focus + result.Attributes.Wisdom;
        result.RankScore.Should().Be(sum);
    }

    [Fact]
    public void RN002_CA001_CapsRankScoreAt48_ForMostAdvancedProfile()
    {
        var result = OnboardingInitialProgressionCalculator.Calculate("advanced", "more_than_3_years");

        result.RankScore.Should().Be(48);
        result.RankCapApplied.Should().BeTrue();
        result.Attributes.Strength.Should().Be(8);
        result.Attributes.Agility.Should().Be(8);
        result.Attributes.Endurance.Should().Be(8);
        result.Attributes.Vitality.Should().Be(8);
        result.Attributes.Focus.Should().Be(8);
        result.Attributes.Wisdom.Should().Be(8);
    }

    [Fact]
    public void RN003_RankNeverExceedsB_EvenForMostAdvancedProfile()
    {
        var result = OnboardingInitialProgressionCalculator.Calculate("advanced", "more_than_3_years");

        result.Rank.Should().Be("B");
    }

    [Fact]
    public void RN004_LevelIsAlwaysOne()
    {
        var result = OnboardingInitialProgressionCalculator.Calculate("advanced", "more_than_3_years");

        result.Level.Should().Be(1);
    }

    [Fact]
    public void RN005_UsesConservativeEffectiveLevel_WhenExperienceAndDurationConflict()
    {
        // Declara experiência avançada, mas nunca treinou de fato.
        var result = OnboardingInitialProgressionCalculator.Calculate("advanced", "does_not_train");

        result.Attributes.Strength.Should().Be(1);
        result.RankScore.Should().Be(6);
        result.RankCapApplied.Should().BeFalse();
    }

    [Fact]
    public void MinimumProfile_SedentaryAndDoesNotTrain_StartsAtRankE()
    {
        var result = OnboardingInitialProgressionCalculator.Calculate("sedentary", "does_not_train");

        result.RankScore.Should().Be(6);
        result.Rank.Should().Be("E");
        result.RankCapApplied.Should().BeFalse();
    }

    [Theory]
    [InlineData("unknown_level", "1_6_months")]
    [InlineData("beginner", "unknown_duration")]
    public void UnknownValues_FallBackToMinimumConservativeLevel(string experienceLevel, string trainingDuration)
    {
        var result = OnboardingInitialProgressionCalculator.Calculate(experienceLevel, trainingDuration);

        result.Attributes.Strength.Should().Be(1);
        result.RankScore.Should().Be(6);
    }
}
