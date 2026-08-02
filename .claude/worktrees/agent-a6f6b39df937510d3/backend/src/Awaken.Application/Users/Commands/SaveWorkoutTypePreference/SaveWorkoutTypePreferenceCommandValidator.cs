using FluentValidation;

namespace Awaken.Application.Users.Commands.SaveWorkoutTypePreference;

public class SaveWorkoutTypePreferenceCommandValidator : AbstractValidator<SaveWorkoutTypePreferenceCommand>
{
    private static readonly string[] ValidTrainingTypes =
        ["personalized_individual", "regeneration", "program"];

    private static readonly string[] ValidProgramIds =
        ["saitama_path", "perfect_2"];

    public SaveWorkoutTypePreferenceCommandValidator()
    {
        RuleFor(x => x.PreferredTrainingType)
            .NotEmpty()
            .Must(t => ValidTrainingTypes.Contains(t))
            .WithMessage($"Tipo de treino invalido. Tipos aceitos: {string.Join(", ", ValidTrainingTypes)}.");

        When(x => x.PreferredTrainingType == "program", () =>
        {
            RuleFor(x => x.PreferredProgramId)
                .NotEmpty()
                .WithMessage("PreferredProgramId e obrigatorio para o tipo 'program'.")
                .Must(id => ValidProgramIds.Contains(id))
                .WithMessage($"Programa invalido. Programas aceitos: {string.Join(", ", ValidProgramIds)}.");
        });
    }
}
