using FluentValidation;

namespace Awaken.Application.Quests.Commands.ChangeTrainingType;

public class ChangeTrainingTypeCommandValidator : AbstractValidator<ChangeTrainingTypeCommand>
{
    private static readonly string[] ValidTrainingTypes =
        ["personalized_individual", "regeneration", "program"];

    private static readonly string[] ValidProgramIds =
        ["saitama_path", "perfect_2"];

    public ChangeTrainingTypeCommandValidator()
    {
        RuleFor(x => x.TrainingType)
            .NotEmpty()
            .Must(t => ValidTrainingTypes.Contains(t))
            .WithMessage($"Tipo de treino inválido. Tipos aceitos: {string.Join(", ", ValidTrainingTypes)}.");

        When(x => x.TrainingType == "program", () =>
        {
            RuleFor(x => x.ProgramId)
                .NotEmpty()
                .WithMessage("ProgramId é obrigatório para o tipo 'program'.")
                .Must(id => ValidProgramIds.Contains(id))
                .WithMessage($"Programa inválido. Programas aceitos: {string.Join(", ", ValidProgramIds)}.");
        });
    }
}
