using Awaken.Application.Quests.Common;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

public class QuestExerciseSeedMapperTests
{
    [Fact]
    public void ParseSeeds_WithAttributeContribution_CalculatesXpRewardByFormula()
    {
        const string json = """
            {
              "exercises": [
                {
                  "id": "ex-1",
                  "name": "Squat",
                  "sets": 3,
                  "repsMin": 8,
                  "repsMax": 12,
                  "restSeconds": 60,
                  "targetRpe": "8",
                  "videoUrl": "https://video.example/squat",
                  "attributeContribution": {
                    "strengthXp": 10, "agilityXp": 0, "enduranceXp": 0,
                    "vitalityXp": 0, "focusXp": 0, "wisdomXp": 1
                  }
                }
              ]
            }
            """;

        var seeds = QuestExerciseSeedMapper.ParseSeeds(json);

        seeds.Should().HaveCount(1);
        var seed = seeds[0];
        seed.Name.Should().Be("Squat");
        seed.ExerciseCatalogProviderId.Should().Be("ex-1");
        seed.Sets.Should().Be(3);
        seed.RepsMin.Should().Be(8);
        seed.RepsMax.Should().Be(12);
        seed.RestSeconds.Should().Be(60);
        seed.TargetRpe.Should().Be("8");
        seed.VideoUrl.Should().Be("https://video.example/squat");
        seed.StrengthXp.Should().Be(10);
        seed.WisdomXp.Should().Be(1);
        // baseXp = 11; xpReward = round(11 * 3 * 8 / 10.0) = round(26.4) = 26
        seed.XpReward.Should().Be(26);
    }

    [Fact]
    public void ParseSeeds_FallbackWithoutAttributeContribution_UsesBaseXpOneAndWisdomOne()
    {
        const string json = """
            {
              "exercises": [
                { "name": "Squat", "sets": 3, "repsMin": 8, "repsMax": 12, "restSeconds": 60, "targetRpe": "8" }
              ]
            }
            """;

        var seeds = QuestExerciseSeedMapper.ParseSeeds(json);

        seeds.Should().HaveCount(1);
        var seed = seeds[0];
        seed.ExerciseCatalogProviderId.Should().BeNull();
        seed.StrengthXp.Should().Be(0);
        seed.AgilityXp.Should().Be(0);
        seed.EnduranceXp.Should().Be(0);
        seed.VitalityXp.Should().Be(0);
        seed.FocusXp.Should().Be(0);
        seed.WisdomXp.Should().Be(1);
        // baseXp = 1; xpReward = round(1 * 3 * 8 / 10.0) = round(2.4) = 2
        seed.XpReward.Should().Be(2);
    }

    [Fact]
    public void ParseSeeds_OldRepsOnlyFormat_FallsBackToRepsAsRepsMin()
    {
        const string json = """
            { "exercises": [ { "name": "Squat", "sets": 2, "reps": 10 } ] }
            """;

        var seeds = QuestExerciseSeedMapper.ParseSeeds(json);

        seeds.Should().HaveCount(1);
        seeds[0].RepsMin.Should().Be(10);
    }

    [Fact]
    public void ParseSeeds_PrefersGifUrlOverVideoUrl()
    {
        const string json = """
            {
              "exercises": [
                {
                  "name": "Squat",
                  "sets": 2,
                  "repsMin": 10,
                  "videoUrl": "https://video.example/squat.mp4",
                  "gifUrl": "https://cdn.awaken.app/squat/360.gif"
                }
              ]
            }
            """;

        var seeds = QuestExerciseSeedMapper.ParseSeeds(json);

        seeds.Should().HaveCount(1);
        seeds[0].VideoUrl.Should().Be("https://cdn.awaken.app/squat/360.gif");
    }

    [Fact]
    public void ParseSeeds_XpRewardHasFloorOfOne()
    {
        const string json = """
            { "exercises": [ { "name": "Squat", "sets": 0, "repsMin": 0 } ] }
            """;

        var seeds = QuestExerciseSeedMapper.ParseSeeds(json);

        seeds.Should().HaveCount(1);
        seeds[0].XpReward.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ not valid json")]
    public void ParseSeeds_NullEmptyOrInvalidJson_ReturnsEmptyListWithoutThrowing(string? workoutJson)
    {
        var act = () => QuestExerciseSeedMapper.ParseSeeds(workoutJson);

        act.Should().NotThrow();
        act().Should().BeEmpty();
    }
}
