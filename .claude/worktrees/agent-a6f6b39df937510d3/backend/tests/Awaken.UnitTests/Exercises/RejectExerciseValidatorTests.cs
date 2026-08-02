using Awaken.Application.Exercises.Commands.RejectExercise;
using FluentAssertions;

namespace Awaken.UnitTests.Exercises;

public class RejectExerciseValidatorTests
{
    private readonly RejectExerciseValidator _validator = new();

    [Fact]
    public void FailsWhenExerciseCatalogIdIsEmpty()
    {
        var result = _validator.Validate(new RejectExerciseCommand(Guid.Empty, "motivo"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExerciseCatalogId");
    }

    [Fact]
    public void FailsWhenReasonIsEmpty()
    {
        var result = _validator.Validate(new RejectExerciseCommand(Guid.NewGuid(), string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void FailsWhenReasonExceedsMaximumLength()
    {
        var result = _validator.Validate(new RejectExerciseCommand(Guid.NewGuid(), new string('a', 1025)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void PassesWithValidIdAndReason()
    {
        var result = _validator.Validate(new RejectExerciseCommand(Guid.NewGuid(), "sem sentido"));

        result.IsValid.Should().BeTrue();
    }
}
