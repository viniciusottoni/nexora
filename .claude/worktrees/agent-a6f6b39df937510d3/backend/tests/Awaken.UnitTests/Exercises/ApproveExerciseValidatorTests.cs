using Awaken.Application.Exercises.Commands.ApproveExercise;
using FluentAssertions;

namespace Awaken.UnitTests.Exercises;

public class ApproveExerciseValidatorTests
{
    private readonly ApproveExerciseValidator _validator = new();

    [Fact]
    public void FailsWhenExerciseCatalogIdIsEmpty()
    {
        var result = _validator.Validate(new ApproveExerciseCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExerciseCatalogId");
    }

    [Fact]
    public void PassesWithValidId()
    {
        var result = _validator.Validate(new ApproveExerciseCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
