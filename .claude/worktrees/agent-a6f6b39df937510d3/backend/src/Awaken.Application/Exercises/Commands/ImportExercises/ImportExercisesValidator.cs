using FluentValidation;

namespace Awaken.Application.Exercises.Commands.ImportExercises;

public class ImportExercisesValidator : AbstractValidator<ImportExercisesCommand>
{
    public ImportExercisesValidator()
    {
        RuleFor(x => x.BatchKey)
            .NotEmpty();

        RuleFor(x => x.Provider)
            .NotEmpty();

        RuleFor(x => x.MaxFiles)
            .GreaterThan(0)
            .LessThanOrEqualTo(50000)
            .When(x => x.MaxFiles.HasValue);
    }
}
