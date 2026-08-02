using Awaken.Application.Onboarding.Commands.SavePhysicalData;
using FluentAssertions;

namespace Awaken.UnitTests.Onboarding;

public class SavePhysicalDataValidatorTests
{
    private readonly SavePhysicalDataCommandValidator _validator = new();

    private static SavePhysicalDataCommand ValidCommand() =>
        new(
            Age: 28,
            HeightCm: 175m,
            WeightKg: 82m,
            BiologicalSex: "masculino",
            TrainingDuration: "1_6_months",
            AvailableMinutesPerWorkout: 30,
            BodyType: null,
            PhysicalLimitations: null,
            PhysicalPains: null);

    [Fact]
    public async Task ValidCommandPassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyRequestFailsValidation()
    {
        var result = await _validator.ValidateAsync(new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            AvailableMinutesPerWorkout: null,
            BodyType: null,
            PhysicalLimitations: null,
            PhysicalPains: null));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(9)]
    [InlineData(121)]
    public async Task AgeForbiddenOutsideRange(int age)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Age = age });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Age");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(120)]
    public async Task AgeBoundaryValuesAreValid(int age)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Age = age });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(49)]
    [InlineData(301)]
    public async Task HeightCmForbiddenOutsideRange(int heightCm)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { HeightCm = heightCm });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HeightCm");
    }

    [Theory]
    [InlineData(50)]
    [InlineData(300)]
    public async Task HeightCmBoundaryValuesAreValid(int heightCm)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { HeightCm = heightCm });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(19)]
    [InlineData(501)]
    public async Task WeightKgForbiddenOutsideRange(int weightKg)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { WeightKg = weightKg });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WeightKg");
    }

    [Theory]
    [InlineData(20)]
    [InlineData(500)]
    public async Task WeightKgBoundaryValuesAreValid(int weightKg)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { WeightKg = weightKg });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyBiologicalSexFails()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { BiologicalSex = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BiologicalSex");
    }

    [Fact]
    public async Task BiologicalSexOver100CharsFails()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { BiologicalSex = new string('x', 101) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BiologicalSex");
    }

    [Fact]
    public async Task FreeTextBiologicalSexIsAccepted()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { BiologicalSex = "nao-binario" });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("does_not_train")]
    [InlineData("less_than_1_month")]
    [InlineData("1_6_months")]
    [InlineData("6_12_months")]
    [InlineData("more_than_1_year")]
    [InlineData("more_than_3_years")]
    public async Task TrainingDurationAcceptedWhenInAllowedSet(string trainingDuration)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { TrainingDuration = trainingDuration });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TrainingDurationRejectedWhenOutsideAllowedSet()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { TrainingDuration = "2_years" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TrainingDuration");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    public async Task AvailableMinutesAcceptedWhenInAllowedSet(int availableMinutesPerWorkout)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            AvailableMinutesPerWorkout = availableMinutesPerWorkout
        });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(55)]
    public async Task AvailableMinutesRejectedWhenOutsideAllowedSet(int availableMinutesPerWorkout)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            AvailableMinutesPerWorkout = availableMinutesPerWorkout
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AvailableMinutesPerWorkout");
    }

    [Theory]
    [InlineData("lean")]
    [InlineData("normal")]
    [InlineData("overweight")]
    [InlineData("athletic_strong")]
    public async Task US141_BodyTypeAcceptedWhenInAllowedSet(string bodyType)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { BodyType = bodyType });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task US141_BodyTypeRejectedWhenOutsideAllowedSet()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { BodyType = "obese" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BodyType");
    }

    [Fact]
    public async Task US141_ValidCommandWithOnlyBodyTypePasses()
    {
        var command = new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            AvailableMinutesPerWorkout: null,
            BodyType: "athletic_strong",
            PhysicalLimitations: null,
            PhysicalPains: null);

        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task US141_NullBodyTypeIsIgnored()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { BodyType = null });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("no_limitations")]
    [InlineData("disk_herniation")]
    [InlineData("knee_problem")]
    [InlineData("no_impact")]
    [InlineData("shoulder_injury")]
    [InlineData("chronic_lumbar_pain")]
    [InlineData("medical_restriction")]
    public async Task US142_PhysicalLimitationTagAcceptedWhenInAllowedSet(string tag)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            PhysicalLimitations = [tag]
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task US142_PhysicalLimitationsRejectedWhenContainsInvalidTag()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            PhysicalLimitations = ["unknown_tag"]
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhysicalLimitations");
    }

    [Fact]
    public async Task US142_EmptyPhysicalLimitationsListIsRejected()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            PhysicalLimitations = []
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhysicalLimitations");
    }

    [Fact]
    public async Task US142_MultipleValidLimitationTagsAccepted()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            PhysicalLimitations = ["knee_problem", "no_impact"]
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task US142_ValidCommandWithOnlyPhysicalLimitationsPasses()
    {
        var command = new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            AvailableMinutesPerWorkout: null,
            BodyType: null,
            PhysicalLimitations: ["no_limitations"],
            PhysicalPains: null);

        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task US142_NullPhysicalLimitationsIsIgnored()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { PhysicalLimitations = null });
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("no_pains")]
    [InlineData("neck")]
    [InlineData("shoulder")]
    [InlineData("wrist")]
    [InlineData("back")]
    [InlineData("lower_back")]
    [InlineData("knees")]
    public async Task US030_PhysicalPainTagAcceptedWhenInAllowedSet(string tag)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            PhysicalPains = [tag]
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task US030_PhysicalPainsRejectedWhenContainsInvalidTag()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            PhysicalPains = ["unknown_pain"]
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhysicalPains");
    }

    [Fact]
    public async Task US030_EmptyPhysicalPainsListIsRejected()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            PhysicalPains = []
        });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhysicalPains");
    }

    [Fact]
    public async Task US030_MultipleValidPainTagsAccepted()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with
        {
            PhysicalPains = ["neck", "shoulder", "knees"]
        });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task US030_ValidCommandWithOnlyPhysicalPainsPasses()
    {
        var command = new SavePhysicalDataCommand(
            Age: null,
            HeightCm: null,
            WeightKg: null,
            BiologicalSex: null,
            TrainingDuration: null,
            AvailableMinutesPerWorkout: null,
            BodyType: null,
            PhysicalLimitations: null,
            PhysicalPains: ["lower_back"]);

        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task US030_NullPhysicalPainsIsIgnored()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { PhysicalPains = null });
        result.IsValid.Should().BeTrue();
    }
}
