using FluentValidation;

namespace Awaken.Application.Onboarding.Commands.SavePhysicalData;

public class SavePhysicalDataCommandValidator : AbstractValidator<SavePhysicalDataCommand>
{
    private static readonly string[] AllowedTrainingDurations =
    [
        "does_not_train",
        "less_than_1_month",
        "1_6_months",
        "6_12_months",
        "more_than_1_year",
        "more_than_3_years"
    ];

    private static readonly int[] AllowedAvailableMinutes = [10, 20, 30, 40, 50];

    private static readonly string[] AllowedBodyTypes =
    [
        "lean",
        "normal",
        "overweight",
        "athletic_strong"
    ];

    // Provisional until EPIC-005 catalog defines final tags
    private static readonly string[] AllowedPhysicalLimitationTags =
    [
        "no_limitations",
        "disk_herniation",
        "knee_problem",
        "no_impact",
        "shoulder_injury",
        "chronic_lumbar_pain",
        "medical_restriction"
    ];

    private static readonly string[] AllowedPhysicalPainTags =
    [
        "no_pains",
        "neck",
        "shoulder",
        "wrist",
        "back",
        "lower_back",
        "knees"
    ];

    public SavePhysicalDataCommandValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneField)
            .WithMessage("Pelo menos um campo de onboarding deve ser informado.");

        When(HasAnyPhysicalField, () =>
        {
            RuleFor(x => x.Age)
                .NotNull()
                .WithMessage("Idade e obrigatoria quando dados fisicos sao enviados.")
                .InclusiveBetween(10, 120)
                .WithMessage("Idade deve estar entre 10 e 120 anos.");

            RuleFor(x => x.HeightCm)
                .NotNull()
                .WithMessage("Altura e obrigatoria quando dados fisicos sao enviados.")
                .InclusiveBetween(50m, 300m)
                .WithMessage("Altura deve estar entre 50 e 300 cm.");

            RuleFor(x => x.WeightKg)
                .NotNull()
                .WithMessage("Peso e obrigatorio quando dados fisicos sao enviados.")
                .InclusiveBetween(20m, 500m)
                .WithMessage("Peso deve estar entre 20 e 500 kg.");

            RuleFor(x => x.BiologicalSex)
                .NotEmpty()
                .WithMessage("Sexo biologico e obrigatorio quando dados fisicos sao enviados.")
                .MaximumLength(100)
                .WithMessage("Sexo biologico deve ter no maximo 100 caracteres.");
        });

        When(x => x.TrainingDuration is not null, () =>
        {
            RuleFor(x => x.TrainingDuration!)
                .Must(value => AllowedTrainingDurations.Contains(value))
                .WithMessage("TrainingDuration deve estar entre as opcoes permitidas.");
        });

        When(x => x.AvailableMinutesPerWorkout.HasValue, () =>
        {
            RuleFor(x => x.AvailableMinutesPerWorkout)
                .Must(value => value is not null && AllowedAvailableMinutes.Contains(value.Value))
                .WithMessage("AvailableMinutesPerWorkout deve estar entre as opcoes permitidas.");
        });

        When(x => x.BodyType is not null, () =>
        {
            RuleFor(x => x.BodyType!)
                .Must(value => AllowedBodyTypes.Contains(value))
                .WithMessage("BodyType deve estar entre as opcoes permitidas: lean, normal, overweight, athletic_strong.");
        });

        When(x => x.PhysicalLimitations is not null, () =>
        {
            RuleFor(x => x.PhysicalLimitations!)
                .Must(tags => tags.Count > 0)
                .WithMessage("PhysicalLimitations nao pode ser uma lista vazia.")
                .Must(tags => tags.All(t => AllowedPhysicalLimitationTags.Contains(t)))
                .WithMessage("PhysicalLimitations contem tags invalidas.");
        });

        When(x => x.PhysicalPains is not null, () =>
        {
            RuleFor(x => x.PhysicalPains!)
                .Must(tags => tags.Count > 0)
                .WithMessage("PhysicalPains nao pode ser uma lista vazia.")
                .Must(tags => tags.All(t => AllowedPhysicalPainTags.Contains(t)))
                .WithMessage("PhysicalPains contem tags invalidas.");
        });
    }

    private static bool HasAtLeastOneField(SavePhysicalDataCommand command) =>
        command.Age.HasValue ||
        command.HeightCm.HasValue ||
        command.WeightKg.HasValue ||
        command.BiologicalSex is not null ||
        command.TrainingDuration is not null ||
        command.AvailableMinutesPerWorkout.HasValue ||
        command.BodyType is not null ||
        command.PhysicalLimitations is not null ||
        command.PhysicalPains is not null;

    private static bool HasAnyPhysicalField(SavePhysicalDataCommand command) =>
        command.Age.HasValue ||
        command.HeightCm.HasValue ||
        command.WeightKg.HasValue ||
        command.BiologicalSex is not null;
}
