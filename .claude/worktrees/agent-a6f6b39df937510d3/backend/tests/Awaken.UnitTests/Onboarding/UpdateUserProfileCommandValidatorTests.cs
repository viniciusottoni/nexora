using Awaken.Application.Onboarding.Commands.UpdateUserProfile;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Onboarding;

public class UpdateUserProfileCommandValidatorTests
{
    private readonly UpdateUserProfileCommandValidator _sut = new();

    private static UpdateUserProfileCommand ValidCommand() => new(
        Goal: "gain_strength",
        ExperienceLevel: "intermediate",
        Age: 30,
        HeightCm: 180m,
        WeightKg: 80m,
        BiologicalSex: "masculino",
        TrainingDuration: "6_12_months",
        TrainingLocation: "gym",
        EquipmentAvailable: ["dumbbells", "full_gym"],
        AvailableMinutesPerWorkout: 40,
        AvailableDaysPerWeek: 4,
        BodyType: "athletic_strong",
        PhysicalLimitations: ["knee_problem"],
        PhysicalPains: ["lower_back"],
        TrainingPreferences: ["low_impact", "strength_focus"]);

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _sut.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCommand_FailsValidation_RequiresAtLeastOneField()
    {
        var command = new UpdateUserProfileCommand(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrors();
    }

    [Fact]
    public void EmptyGoal_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Goal = "" });
        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void InvalidGoal_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Goal = "become_superhero" });
        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void NullGoal_DoesNotFailValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Goal = null });
        result.ShouldNotHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void EmptyExperienceLevel_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { ExperienceLevel = "" });
        result.ShouldHaveValidationErrorFor(x => x.ExperienceLevel);
    }

    [Fact]
    public void InvalidExperienceLevel_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { ExperienceLevel = "god_mode" });
        result.ShouldHaveValidationErrorFor(x => x.ExperienceLevel);
    }

    [Fact]
    public void AgeBelow10_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Age = 9 });
        result.ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Fact]
    public void AgeAbove120_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Age = 121 });
        result.ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Fact]
    public void HeightOutOfRange_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { HeightCm = 1m });
        result.ShouldHaveValidationErrorFor(x => x.HeightCm);
    }

    [Fact]
    public void WeightOutOfRange_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { WeightKg = 1000m });
        result.ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void EmptyBiologicalSex_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { BiologicalSex = "" });
        result.ShouldHaveValidationErrorFor(x => x.BiologicalSex);
    }

    [Fact]
    public void InvalidTrainingDuration_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { TrainingDuration = "2_years" });
        result.ShouldHaveValidationErrorFor(x => x.TrainingDuration);
    }

    [Fact]
    public void InvalidAvailableMinutes_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { AvailableMinutesPerWorkout = 15 });
        result.ShouldHaveValidationErrorFor(x => x.AvailableMinutesPerWorkout);
    }

    [Fact]
    public void InvalidAvailableDaysPerWeek_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { AvailableDaysPerWeek = 8 });
        result.ShouldHaveValidationErrorFor(x => x.AvailableDaysPerWeek);
    }

    [Fact]
    public void EmptyEquipmentAvailable_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { EquipmentAvailable = [] });
        result.ShouldHaveValidationErrorFor(x => x.EquipmentAvailable);
    }

    [Fact]
    public void EmptyTrainingPreferences_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { TrainingPreferences = [] });
        result.ShouldHaveValidationErrorFor(x => x.TrainingPreferences);
    }

    [Fact]
    public void InvalidBodyType_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { BodyType = "obese" });
        result.ShouldHaveValidationErrorFor(x => x.BodyType);
    }

    [Fact]
    public void EmptyPhysicalLimitations_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalLimitations = [] });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalLimitations);
    }

    [Fact]
    public void InvalidPhysicalLimitationTag_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalLimitations = ["unknown"] });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalLimitations);
    }

    [Fact]
    public void EmptyPhysicalPains_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalPains = [] });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalPains);
    }

    [Fact]
    public void InvalidPhysicalPainTag_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalPains = ["unknown"] });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalPains);
    }

    [Fact]
    public void NullPhysicalFields_DoNotFailValidation()
    {
        var command = ValidCommand() with
        {
            PhysicalLimitations = null,
            PhysicalPains = null,
            TrainingDuration = null,
            TrainingLocation = null,
            EquipmentAvailable = null,
            AvailableMinutesPerWorkout = null,
            AvailableDaysPerWeek = null,
            BodyType = null,
            TrainingPreferences = null,
        };
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.PhysicalLimitations);
        result.ShouldNotHaveValidationErrorFor(x => x.PhysicalPains);
        result.ShouldNotHaveValidationErrorFor(x => x.TrainingPreferences);
    }
}
