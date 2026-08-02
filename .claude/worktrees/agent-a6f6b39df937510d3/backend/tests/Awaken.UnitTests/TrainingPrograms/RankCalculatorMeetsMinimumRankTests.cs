using Awaken.Domain.Services.Progression;
using FluentAssertions;

namespace Awaken.UnitTests.TrainingPrograms;

/// <summary>
/// US-231 RN-002/RN-004: comparacao de ranks em ordem crescente
/// (E &lt; D &lt; C &lt; B &lt; A &lt; S &lt; SS &lt; SSS).
/// </summary>
public class RankCalculatorMeetsMinimumRankTests
{
    [Theory]
    [InlineData("E", "E+", true)]
    [InlineData("D", "E+", true)]
    [InlineData("SSS", "E+", true)]
    [InlineData("E", "D+", false)]
    [InlineData("D", "D+", true)]
    [InlineData("C", "D+", true)]
    [InlineData("D", "C+", false)]
    [InlineData("SSS", "SSS+", true)]
    [InlineData("SS", "SSS+", false)]
    [InlineData("E", "SSS+", false)]
    public void MeetsMinimumRank_ReturnsExpected_ForKnownRanks(
        string userRank, string minimumRank, bool expected)
    {
        RankCalculator.MeetsMinimumRank(userRank, minimumRank).Should().Be(expected);
    }

    [Theory]
    [InlineData("Z", "E")]
    [InlineData("E", "Z")]
    [InlineData("", "E")]
    [InlineData("E", "")]
    [InlineData(null, "E")]
    [InlineData("E", null)]
    public void MeetsMinimumRank_ReturnsFalse_ForUnknownRank(string? userRank, string? minimumRank)
    {
        RankCalculator.MeetsMinimumRank(userRank!, minimumRank!).Should().BeFalse();
    }

    [Theory]
    [InlineData("E", "E+")]
    [InlineData("D+", "D+")]
    [InlineData("C", "C+")]
    [InlineData("B+", "B+")]
    public void FormatMinimumRank_ReturnsCatalogLabel(string minimumRank, string expected)
    {
        RankCalculator.FormatMinimumRank(minimumRank).Should().Be(expected);
    }
}
