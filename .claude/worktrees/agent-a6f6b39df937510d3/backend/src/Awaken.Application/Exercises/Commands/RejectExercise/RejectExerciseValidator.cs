using FluentValidation;

namespace Awaken.Application.Exercises.Commands.RejectExercise;

public class RejectExerciseValidator : AbstractValidator<RejectExerciseCommand>
{
    public RejectExerciseValidator()
    {
        RuleFor(x => x.ExerciseCatalogId)
            .NotEmpty();

        // RN-005 (US-149): exercício reprovado precisa de motivo registrado.
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1024);
    }
}
