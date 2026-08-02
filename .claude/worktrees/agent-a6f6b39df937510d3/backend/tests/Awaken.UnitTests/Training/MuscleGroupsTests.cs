using Awaken.Domain.Entities.Training;
using FluentAssertions;

namespace Awaken.UnitTests.Training;

// US-237: MuscleGroups é o enum interno (US-036) de grupos musculares usado
// para validar o split map — grupo fora desta lista bloqueia o seed (RN-005).
public class MuscleGroupsTests
{
    [Theory]
    [InlineData(MuscleGroups.Chest)]
    [InlineData(MuscleGroups.Back)]
    [InlineData(MuscleGroups.Shoulders)]
    [InlineData(MuscleGroups.Biceps)]
    [InlineData(MuscleGroups.Triceps)]
    [InlineData(MuscleGroups.Forearms)]
    [InlineData(MuscleGroups.Traps)]
    [InlineData(MuscleGroups.RearDelts)]
    [InlineData(MuscleGroups.Quadriceps)]
    [InlineData(MuscleGroups.Hamstrings)]
    [InlineData(MuscleGroups.Glutes)]
    [InlineData(MuscleGroups.Calves)]
    [InlineData(MuscleGroups.Core)]
    public void IsValid_ReturnsTrue_ForKnownMuscleGroups(string muscleGroup)
    {
        MuscleGroups.IsValid(muscleGroup).Should().BeTrue();
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForUnknownMuscleGroup()
    {
        MuscleGroups.IsValid("bogus_muscle").Should().BeFalse();
    }
}
