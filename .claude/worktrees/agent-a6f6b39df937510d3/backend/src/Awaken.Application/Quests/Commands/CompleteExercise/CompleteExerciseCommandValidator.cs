using FluentValidation;

namespace Awaken.Application.Quests.Commands.CompleteExercise;

public class CompleteExerciseCommandValidator : AbstractValidator<CompleteExerciseCommand>
{
    public CompleteExerciseCommandValidator()
    {
        RuleFor(x => x.QuestId)
            .NotEmpty().WithMessage("QuestId e obrigatorio.");

        RuleFor(x => x.QuestExerciseId)
            .NotEmpty().WithMessage("QuestExerciseId e obrigatorio.");

        RuleFor(x => x.SetsCompleted)
            .GreaterThan(0).WithMessage("SetsCompleted deve ser maior que zero.");
    }
}
