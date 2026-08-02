using FluentValidation;

namespace Awaken.Application.Quests.Queries.ValidateTrainingTypeChange;

public class ValidateTrainingTypeChangeQueryValidator : AbstractValidator<ValidateTrainingTypeChangeQuery>
{
    private static readonly string[] ValidTrainingTypes =
        ["personalized_individual", "regeneration", "program"];

    private static readonly string[] ValidProgramIds =
        ["saitama_path", "perfect_2"];

    public ValidateTrainingTypeChangeQueryValidator()
    {
        RuleFor(x => x.TrainingType)
            .NotEmpty()
            .Must(t => ValidTrainingTypes.Contains(t))
            .WithMessage($"Tipo de treino invalido. Tipos aceitos: {string.Join(", ", ValidTrainingTypes)}.");

        When(x => x.TrainingType == "program", () =>
        {
            RuleFor(x => x.ProgramId)
                .NotEmpty()
                .WithMessage("ProgramId e obrigatorio para o tipo 'program'.")
                .Must(id => ValidProgramIds.Contains(id))
                .WithMessage($"Programa invalido. Programas aceitos: {string.Join(", ", ValidProgramIds)}.");
        });
    }
}
