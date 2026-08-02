using Awaken.Application.Quests.Commands.ChangeTrainingType;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Quests;

public class ChangeTrainingTypeCommandValidatorTests
{
    private readonly ChangeTrainingTypeCommandValidator _validator = new();

    private static ChangeTrainingTypeCommand Cmd(string type, string? programId = null) =>
        new(Guid.NewGuid(), type, programId);

    [Theory]
    [InlineData("personalized_individual", null)]
    [InlineData("regeneration", null)]
    [InlineData("program", "saitama_path")]
    [InlineData("program", "perfect_2")]
    public async Task Valid_WhenTrainingTypeAndProgramAreCorrect(string type, string? programId)
    {
        var result = await _validator.TestValidateAsync(Cmd(type, programId));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── RN-003 / RN-004 (US-051) ──────────────────────────────────────────────

    [Fact]
    public async Task Invalid_WhenTrainingTypeIsUnknown()
    {
        var result = await _validator.TestValidateAsync(Cmd("free_edit"));
        result.ShouldHaveValidationErrorFor(x => x.TrainingType);
    }

    [Fact]
    public async Task Invalid_WhenTypeIsProgram_AndProgramIdIsMissing()
    {
        var result = await _validator.TestValidateAsync(Cmd("program", null));
        result.ShouldHaveValidationErrorFor(x => x.ProgramId);
    }

    [Fact]
    public async Task Invalid_WhenTypeIsProgram_AndProgramIdIsUnknown()
    {
        var result = await _validator.TestValidateAsync(Cmd("program", "saitama_v99"));
        result.ShouldHaveValidationErrorFor(x => x.ProgramId);
    }

    [Fact]
    public async Task Valid_WhenTypeIsRegeneration_AndProgramIdIsProvided()
    {
        // programId deve ser ignorado quando tipo nao é "program"
        var result = await _validator.TestValidateAsync(Cmd("regeneration", "saitama_path"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
