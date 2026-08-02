using FluentValidation;

namespace Awaken.Application.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    private static readonly string[] AllowedGoals =
    [
        "gain_muscle", "lose_weight", "improve_conditioning", "gain_strength", "stay_active"
    ];

    private static readonly string[] AllowedExperienceLevels =
    [
        "sedentary", "beginner", "intermediate", "advanced"
    ];

    private static readonly string[] AllowedTrainingDurations =
    [
        "does_not_train", "less_than_1_month", "1_6_months",
        "6_12_months", "more_than_1_year", "more_than_3_years"
    ];

    private static readonly int[] AllowedAvailableMinutes = [10, 20, 30, 40, 50];

    private static readonly string[] AllowedBodyTypes =
    [
        "lean", "normal", "overweight", "athletic_strong"
    ];

    private static readonly string[] AllowedPhysicalLimitationTags =
    [
        "no_limitations", "disk_herniation", "knee_problem", "no_impact",
        "shoulder_injury", "chronic_lumbar_pain", "medical_restriction"
    ];

    private static readonly string[] AllowedPhysicalPainTags =
    [
        "no_pains", "neck", "shoulder", "wrist", "back", "lower_back", "knees"
    ];

    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.Goal)
            .NotEmpty().WithMessage("Goal e obrigatorio.")
            .Must(v => AllowedGoals.Contains(v))
            .WithMessage("Goal deve ser um dos valores permitidos.");

        RuleFor(x => x.ExperienceLevel)
            .NotEmpty().WithMessage("ExperienceLevel e obrigatorio.")
            .Must(v => AllowedExperienceLevels.Contains(v))
            .WithMessage("ExperienceLevel deve ser um dos valores permitidos.");

        RuleFor(x => x.Age)
            .InclusiveBetween(10, 120)
            .WithMessage("Idade deve estar entre 10 e 120 anos.");

        RuleFor(x => x.HeightCm)
            .InclusiveBetween(50m, 300m)
            .WithMessage("Altura deve estar entre 50 e 300 cm.");

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(20m, 500m)
            .WithMessage("Peso deve estar entre 20 e 500 kg.");

        RuleFor(x => x.BiologicalSex)
            .NotEmpty().WithMessage("Sexo biologico e obrigatorio.")
            .MaximumLength(100);

        RuleFor(x => x.TrainingDuration)
            .Must(v => AllowedTrainingDurations.Contains(v))
            .WithMessage("TrainingDuration invalido.");

        RuleFor(x => x.AvailableMinutesPerWorkout)
            .Must(v => AllowedAvailableMinutes.Contains(v))
            .WithMessage("AvailableMinutesPerWorkout invalido.");

        RuleFor(x => x.BodyType)
            .Must(v => AllowedBodyTypes.Contains(v))
            .WithMessage("BodyType invalido.");

        RuleFor(x => x.PhysicalLimitations)
            .Must(tags => tags.Count > 0)
            .WithMessage("PhysicalLimitations nao pode ser vazia.")
            .Must(tags => tags.All(t => AllowedPhysicalLimitationTags.Contains(t)))
            .WithMessage("PhysicalLimitations contem tags invalidas.");

        RuleFor(x => x.PhysicalPains)
            .Must(tags => tags.Count > 0)
            .WithMessage("PhysicalPains nao pode ser vazia.")
            .Must(tags => tags.All(t => AllowedPhysicalPainTags.Contains(t)))
            .WithMessage("PhysicalPains contem tags invalidas.");
    }
}
