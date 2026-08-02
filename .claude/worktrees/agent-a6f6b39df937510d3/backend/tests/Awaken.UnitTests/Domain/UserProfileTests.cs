using FluentAssertions;
using Awaken.Domain.Entities.Onboarding;

namespace Awaken.UnitTests.Domain;

public class UserProfileTests
{
    [Fact]
    public void Create_WithGoalAndExperienceLevel_SetsBothFields()
    {
        var profile = UserProfile.Create(
            userId: Guid.NewGuid(),
            goal: "gain_muscle",
            experienceLevel: "beginner");

        profile.Goal.Should().Be("gain_muscle");
        profile.ExperienceLevel.Should().Be("beginner");
    }

    [Fact]
    public void Create_WithoutGoalOrLevel_BothAreNull()
    {
        var profile = UserProfile.Create(userId: Guid.NewGuid());

        profile.Goal.Should().BeNull();
        profile.ExperienceLevel.Should().BeNull();
    }

    [Fact]
    public void Create_WithExperienceLevelAndTrainingDuration_ComputesEffectiveExperienceLevel()
    {
        // US-150 CA-001: avancado + nao treina deve resultar em sedentario.
        var profile = UserProfile.Create(
            userId: Guid.NewGuid(),
            experienceLevel: "advanced",
            trainingDuration: "does_not_train");

        profile.EffectiveExperienceLevel.Should().Be("sedentary");
    }

    [Fact]
    public void ApplyPatch_UpdatesGoalAndExperienceLevel()
    {
        var profile = UserProfile.Create(Guid.NewGuid());
        var utcNow = new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        profile.ApplyPatch(
            age: null, heightCm: null, weightKg: null, biologicalSex: null,
            trainingDuration: null, trainingLocation: null,
            equipmentAvailable: null, availableMinutesPerWorkout: null,
            availableDaysPerWeek: null,
            bodyType: null, physicalLimitations: null, physicalPains: null,
            trainingPreferences: null,
            utcNow: utcNow,
            goal: "lose_weight",
            experienceLevel: "intermediate");

        profile.Goal.Should().Be("lose_weight");
        profile.ExperienceLevel.Should().Be("intermediate");
    }

    [Fact]
    public void ApplyPatch_UpdatingExperienceLevelOrTrainingDuration_RecalculatesEffectiveExperienceLevel()
    {
        var profile = UserProfile.Create(
            Guid.NewGuid(),
            experienceLevel: "advanced",
            trainingDuration: "more_than_3_years");
        profile.EffectiveExperienceLevel.Should().Be("advanced");
        var utcNow = new DateTime(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);

        profile.ApplyPatch(
            age: null, heightCm: null, weightKg: null, biologicalSex: null,
            trainingDuration: "does_not_train", trainingLocation: null,
            equipmentAvailable: null, availableMinutesPerWorkout: null,
            availableDaysPerWeek: null,
            bodyType: null, physicalLimitations: null, physicalPains: null,
            trainingPreferences: null,
            utcNow: utcNow);

        profile.TrainingDuration.Should().Be("does_not_train");
        profile.EffectiveExperienceLevel.Should().Be("sedentary");
    }

    [Fact]
    public void ApplyPatch_UpdatesTrainingContextFields()
    {
        var profile = UserProfile.Create(Guid.NewGuid());
        var utcNow = new DateTime(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc);

        profile.ApplyPatch(
            age: null, heightCm: null, weightKg: null, biologicalSex: null,
            trainingDuration: null, trainingLocation: "gym",
            equipmentAvailable: ["dumbbells", "full_gym"],
            availableMinutesPerWorkout: null, availableDaysPerWeek: 4,
            bodyType: null, physicalLimitations: null, physicalPains: null,
            trainingPreferences: ["low_impact", "strength_focus"],
            utcNow: utcNow);

        profile.TrainingLocation.Should().Be("gym");
        profile.EquipmentAvailable.Should().BeEquivalentTo(["dumbbells", "full_gym"]);
        profile.AvailableDaysPerWeek.Should().Be(4);
        profile.TrainingPreferences.Should().BeEquivalentTo(["low_impact", "strength_focus"]);
    }

    [Fact]
    public void ApplyPatch_NullGoalAndLevel_PreservesExisting()
    {
        var profile = UserProfile.Create(
            userId: Guid.NewGuid(),
            goal: "gain_muscle",
            experienceLevel: "beginner");
        var utcNow = new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        profile.ApplyPatch(
            age: 28, heightCm: null, weightKg: null, biologicalSex: null,
            trainingDuration: null, trainingLocation: null,
            equipmentAvailable: null, availableMinutesPerWorkout: null,
            availableDaysPerWeek: null,
            bodyType: null, physicalLimitations: null, physicalPains: null,
            trainingPreferences: null,
            utcNow: utcNow,
            goal: null,
            experienceLevel: null);

        profile.Goal.Should().Be("gain_muscle");
        profile.ExperienceLevel.Should().Be("beginner");
        profile.Age.Should().Be(28);
    }
}
