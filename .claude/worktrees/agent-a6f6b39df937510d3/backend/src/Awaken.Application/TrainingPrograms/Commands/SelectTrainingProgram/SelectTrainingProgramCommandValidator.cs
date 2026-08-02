using FluentValidation;

namespace Awaken.Application.TrainingPrograms.Commands.SelectTrainingProgram;

public class SelectTrainingProgramCommandValidator : AbstractValidator<SelectTrainingProgramCommand>
{
    public SelectTrainingProgramCommandValidator()
    {
        RuleFor(x => x.ProgramKey).NotEmpty().MaximumLength(64);
    }
}
