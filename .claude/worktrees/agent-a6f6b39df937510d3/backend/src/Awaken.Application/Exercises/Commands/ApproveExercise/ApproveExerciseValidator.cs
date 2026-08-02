using FluentValidation;

namespace Awaken.Application.Exercises.Commands.ApproveExercise;

public class ApproveExerciseValidator : AbstractValidator<ApproveExerciseCommand>
{
    public ApproveExerciseValidator()
    {
        RuleFor(x => x.ExerciseCatalogId)
            .NotEmpty();
    }
}
