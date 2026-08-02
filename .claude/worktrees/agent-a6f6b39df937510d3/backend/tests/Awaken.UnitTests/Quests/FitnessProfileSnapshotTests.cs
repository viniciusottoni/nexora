using System.Text.Json;
using Awaken.Application.Quests.Common;
using Awaken.Domain.Entities.Onboarding;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

public class FitnessProfileSnapshotTests
{
    [Fact]
    public void Build_IncludesTrainingDurationAndEffectiveExperienceLevel()
    {
        var profile = UserProfile.Create(
            Guid.NewGuid(),
            experienceLevel: "advanced",
            trainingDuration: "does_not_train");

        var json = FitnessProfileSnapshot.Build(profile);
        using var document = JsonDocument.Parse(json);

        document.RootElement.GetProperty("experienceLevel").GetString().Should().Be("advanced");
        document.RootElement.GetProperty("trainingDuration").GetString().Should().Be("does_not_train");
        document.RootElement.GetProperty("effectiveExperienceLevel").GetString().Should().Be("sedentary");
    }
}
