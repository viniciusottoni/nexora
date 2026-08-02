using Awaken.Application.Exercises.Commands.ImportExercises;
using FluentAssertions;

namespace Awaken.UnitTests.Exercises;

public class ImportExercisesValidatorTests
{
    private readonly ImportExercisesValidator _validator = new();

    [Fact]
    public void FailsWhenBatchKeyIsEmpty()
    {
        var result = _validator.Validate(new ImportExercisesCommand(string.Empty, "local_files", null, false));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BatchKey");
    }

    [Fact]
    public void FailsWhenProviderIsEmpty()
    {
        var result = _validator.Validate(new ImportExercisesCommand("batch-2026-01", string.Empty, null, false));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Fact]
    public void FailsWhenMaxFilesIsZeroOrNegative()
    {
        var result = _validator.Validate(new ImportExercisesCommand("batch-2026-01", "local_files", 0, false));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxFiles");
    }

    [Fact]
    public void FailsWhenMaxFilesExceedsMaximum()
    {
        var result = _validator.Validate(new ImportExercisesCommand("batch-2026-01", "local_files", 50001, false));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxFiles");
    }

    [Fact]
    public void PassesWithValidLocalImportRequest()
    {
        var result = _validator.Validate(new ImportExercisesCommand("batch-2026-01", "local_files", 100, false));

        result.IsValid.Should().BeTrue();
    }
}
