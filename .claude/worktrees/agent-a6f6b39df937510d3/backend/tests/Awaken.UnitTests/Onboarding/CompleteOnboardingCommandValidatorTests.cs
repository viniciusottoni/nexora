using Awaken.Application.Onboarding.Commands.CompleteOnboarding;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Onboarding;

public class CompleteOnboardingCommandValidatorTests
{
    private readonly CompleteOnboardingCommandValidator _sut = new();

    private static CompleteOnboardingCommand ValidCommand() => new(
        Goal: "gain_muscle",
        ExperienceLevel: "beginner",
        Age: 28,
        HeightCm: 175m,
        WeightKg: 82m,
        BiologicalSex: "masculino",
        TrainingDuration: "1_6_months",
        AvailableMinutesPerWorkout: 30,
        BodyType: "normal",
        PhysicalLimitations: ["no_limitations"],
        PhysicalPains: ["no_pains"]);

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        var result = _sut.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("gain_muscle")]
    [InlineData("lose_weight")]
    [InlineData("improve_conditioning")]
    [InlineData("gain_strength")]
    [InlineData("stay_active")]
    public void AllowedGoals_PassValidation(string goal)
    {
        var result = _sut.TestValidate(ValidCommand() with { Goal = goal });
        result.ShouldNotHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void InvalidGoal_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Goal = "become_superhero" });
        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void EmptyGoal_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { Goal = "" });
        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Theory]
    [InlineData("sedentary")]
    [InlineData("beginner")]
    [InlineData("intermediate")]
    [InlineData("advanced")]
    public void AllowedExperienceLevels_PassValidation(string level)
    {
        var result = _sut.TestValidate(ValidCommand() with { ExperienceLevel = level });
        result.ShouldNotHaveValidationErrorFor(x => x.ExperienceLevel);
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
    public void HeightBelow50_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { HeightCm = 49m });
        result.ShouldHaveValidationErrorFor(x => x.HeightCm);
    }

    [Fact]
    public void HeightAbove300_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { HeightCm = 301m });
        result.ShouldHaveValidationErrorFor(x => x.HeightCm);
    }

    [Fact]
    public void WeightBelow20_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { WeightKg = 19m });
        result.ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void WeightAbove500_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { WeightKg = 501m });
        result.ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void EmptyBiologicalSex_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { BiologicalSex = "" });
        result.ShouldHaveValidationErrorFor(x => x.BiologicalSex);
    }

    [Theory]
    [InlineData("does_not_train")]
    [InlineData("less_than_1_month")]
    [InlineData("1_6_months")]
    [InlineData("6_12_months")]
    [InlineData("more_than_1_year")]
    [InlineData("more_than_3_years")]
    public void AllowedTrainingDurations_PassValidation(string duration)
    {
        var result = _sut.TestValidate(ValidCommand() with { TrainingDuration = duration });
        result.ShouldNotHaveValidationErrorFor(x => x.TrainingDuration);
    }

    [Fact]
    public void InvalidTrainingDuration_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { TrainingDuration = "2_years" });
        result.ShouldHaveValidationErrorFor(x => x.TrainingDuration);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    public void AllowedAvailableMinutes_PassValidation(int minutes)
    {
        var result = _sut.TestValidate(ValidCommand() with { AvailableMinutesPerWorkout = minutes });
        result.ShouldNotHaveValidationErrorFor(x => x.AvailableMinutesPerWorkout);
    }

    [Fact]
    public void InvalidAvailableMinutes_FailsValidation()
    {
        var result = _sut.TestValidate(ValidCommand() with { AvailableMinutesPerWorkout = 15 });
        result.ShouldHaveValidationErrorFor(x => x.AvailableMinutesPerWorkout);
    }

    [Theory]
    [InlineData("lean")]
    [InlineData("normal")]
    [InlineData("overweight")]
    [InlineData("athletic_strong")]
    public void AllowedBodyTypes_PassValidation(string bodyType)
    {
        var result = _sut.TestValidate(ValidCommand() with { BodyType = bodyType });
        result.ShouldNotHaveValidationErrorFor(x => x.BodyType);
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

    [Theory]
    [InlineData("no_limitations")]
    [InlineData("disk_herniation")]
    [InlineData("knee_problem")]
    [InlineData("no_impact")]
    [InlineData("shoulder_injury")]
    [InlineData("chronic_lumbar_pain")]
    [InlineData("medical_restriction")]
    public void AllowedLimitationTags_PassValidation(string tag)
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalLimitations = [tag] });
        result.ShouldNotHaveValidationErrorFor(x => x.PhysicalLimitations);
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

    [Theory]
    [InlineData("no_pains")]
    [InlineData("neck")]
    [InlineData("shoulder")]
    [InlineData("wrist")]
    [InlineData("back")]
    [InlineData("lower_back")]
    [InlineData("knees")]
    public void AllowedPainTags_PassValidation(string tag)
    {
        var result = _sut.TestValidate(ValidCommand() with { PhysicalPains = [tag] });
        result.ShouldNotHaveValidationErrorFor(x => x.PhysicalPains);
    }
}
