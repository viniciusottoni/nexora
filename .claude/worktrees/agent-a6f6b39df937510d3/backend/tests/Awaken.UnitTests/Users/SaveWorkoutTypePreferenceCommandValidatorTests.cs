using Awaken.Application.Users.Commands.SaveWorkoutTypePreference;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Users;

public class SaveWorkoutTypePreferenceCommandValidatorTests
{
    private readonly SaveWorkoutTypePreferenceCommandValidator _validator = new();

    [Theory]
    [InlineData("personalized_individual")]
    [InlineData("regeneration")]
    public void Accepts_NonProgramTypes(string type)
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand(type, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_UnknownType()
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand("free_edit", null));
        result.ShouldHaveValidationErrorFor(x => x.PreferredTrainingType);
    }

    [Fact]
    public void Rejects_ProgramTypeWithoutProgramId()
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand("program", null));
        result.ShouldHaveValidationErrorFor(x => x.PreferredProgramId);
    }

    [Fact]
    public void Rejects_InvalidProgramId()
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand("program", "nope"));
        result.ShouldHaveValidationErrorFor(x => x.PreferredProgramId);
    }

    [Theory]
    [InlineData("saitama_path")]
    [InlineData("perfect_2")]
    public void Accepts_ValidProgram(string programId)
    {
        var result = _validator.TestValidate(new SaveWorkoutTypePreferenceCommand("program", programId));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
