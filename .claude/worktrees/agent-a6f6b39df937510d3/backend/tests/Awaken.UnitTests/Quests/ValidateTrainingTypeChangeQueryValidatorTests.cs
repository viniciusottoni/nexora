using Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Quests;

public class ValidateTrainingTypeChangeQueryValidatorTests
{
    private readonly ValidateTrainingTypeChangeQueryValidator _validator = new();

    [Theory]
    [InlineData("personalized_individual")]
    [InlineData("regeneration")]
    public void Accepts_ValidTypesWithoutProgram(string type)
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), type, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_UnknownTrainingType()
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), "free_edit", null));
        result.ShouldHaveValidationErrorFor(x => x.TrainingType);
    }

    [Fact]
    public void Rejects_ProgramTypeWithoutProgramId()
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), "program", null));
        result.ShouldHaveValidationErrorFor(x => x.ProgramId);
    }

    [Theory]
    [InlineData("saitama_path")]
    [InlineData("perfect_2")]
    public void Accepts_ValidProgramIds(string programId)
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), "program", programId));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_InvalidProgramId()
    {
        var result = _validator.TestValidate(
            new ValidateTrainingTypeChangeQuery(Guid.NewGuid(), "program", "nope"));
        result.ShouldHaveValidationErrorFor(x => x.ProgramId);
    }
}
