using Awaken.Domain.Services.Progression;
using FluentAssertions;

namespace Awaken.UnitTests.Domain;

public class RankCalculatorTests
{
    [Theory]
    [InlineData(0, "E")]
    [InlineData(6, "E")]
    [InlineData(17, "E")]
    [InlineData(18, "D")]
    [InlineData(29, "D")]
    [InlineData(30, "C")]
    [InlineData(47, "C")]
    [InlineData(48, "B")]
    [InlineData(83, "B")]
    [InlineData(84, "A")]
    [InlineData(155, "A")]
    [InlineData(156, "S")]
    [InlineData(299, "S")]
    [InlineData(300, "SS")]
    [InlineData(587, "SS")]
    [InlineData(588, "SSS")]
    [InlineData(1000, "SSS")]
    public void CalculateRank_ReturnsExpectedRank_ForEachBoundary(int rankScore, string expectedRank)
    {
        RankCalculator.CalculateRank(rankScore).Should().Be(expectedRank);
    }

    [Fact]
    public void OnboardingRankScoreCap_IsForty8()
    {
        RankCalculator.OnboardingRankScoreCap.Should().Be(48);
    }
}
