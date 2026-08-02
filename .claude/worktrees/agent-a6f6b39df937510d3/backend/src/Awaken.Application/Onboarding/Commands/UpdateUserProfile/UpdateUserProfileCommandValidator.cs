using FluentValidation;

namespace Awaken.Application.Onboarding.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
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

    private static readonly string[] AllowedTrainingLocations =
    [
        "home", "gym", "outdoor"
    ];

    private static readonly string[] AllowedEquipmentTags =
    [
        "bodyweight", "dumbbells", "full_gym", "home_equipment"
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

    private static readonly string[] AllowedTrainingPreferenceTags =
    [
        "low_impact", "strength_focus", "mobility", "short_sessions"
    ];

    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneField)
            .WithMessage("Pelo menos um campo de perfil deve ser informado.");

        When(x => x.Goal is not null, () =>
        {
            RuleFor(x => x.Goal!)
                .NotEmpty().WithMessage("Goal nao pode ficar vazio.")
                .Must(v => AllowedGoals.Contains(v))
                .WithMessage("Goal deve ser um dos valores permitidos.");
        });

        When(x => x.ExperienceLevel is not null, () =>
        {
            RuleFor(x => x.ExperienceLevel!)
                .NotEmpty().WithMessage("ExperienceLevel nao pode ficar vazio.")
                .Must(v => AllowedExperienceLevels.Contains(v))
                .WithMessage("ExperienceLevel deve ser um dos valores permitidos.");
        });

        When(x => x.Age.HasValue, () =>
        {
            RuleFor(x => x.Age)
                .InclusiveBetween(10, 120)
                .WithMessage("Idade deve estar entre 10 e 120 anos.");
        });

        When(x => x.HeightCm.HasValue, () =>
        {
            RuleFor(x => x.HeightCm)
                .InclusiveBetween(50m, 300m)
                .WithMessage("Altura deve estar entre 50 e 300 cm.");
        });

        When(x => x.WeightKg.HasValue, () =>
        {
            RuleFor(x => x.WeightKg)
                .InclusiveBetween(20m, 500m)
                .WithMessage("Peso deve estar entre 20 e 500 kg.");
        });

        When(x => x.BiologicalSex is not null, () =>
        {
            RuleFor(x => x.BiologicalSex!)
                .NotEmpty().WithMessage("Sexo biologico nao pode ficar vazio.")
                .MaximumLength(100);
        });

        When(x => x.TrainingDuration is not null, () =>
        {
            RuleFor(x => x.TrainingDuration!)
                .Must(v => AllowedTrainingDurations.Contains(v))
                .WithMessage("TrainingDuration invalido.");
        });

        When(x => x.TrainingLocation is not null, () =>
        {
            RuleFor(x => x.TrainingLocation!)
                .NotEmpty().WithMessage("TrainingLocation nao pode ficar vazio.")
                .Must(v => AllowedTrainingLocations.Contains(v))
                .WithMessage("TrainingLocation invalido.");
        });

        When(x => x.EquipmentAvailable is not null, () =>
        {
            RuleFor(x => x.EquipmentAvailable!)
                .Must(tags => tags.Count > 0)
                .WithMessage("EquipmentAvailable nao pode ser vazio.")
                .Must(tags => tags.All(t => AllowedEquipmentTags.Contains(t)))
                .WithMessage("EquipmentAvailable contem tags invalidas.");
        });

        When(x => x.AvailableMinutesPerWorkout.HasValue, () =>
        {
            RuleFor(x => x.AvailableMinutesPerWorkout)
                .Must(v => v is not null && AllowedAvailableMinutes.Contains(v.Value))
                .WithMessage("AvailableMinutesPerWorkout invalido.");
        });

        When(x => x.AvailableDaysPerWeek.HasValue, () =>
        {
            RuleFor(x => x.AvailableDaysPerWeek)
                .InclusiveBetween(1, 7)
                .WithMessage("AvailableDaysPerWeek deve estar entre 1 e 7.");
        });

        When(x => x.BodyType is not null, () =>
        {
            RuleFor(x => x.BodyType!)
                .Must(v => AllowedBodyTypes.Contains(v))
                .WithMessage("BodyType invalido.");
        });

        When(x => x.PhysicalLimitations is not null, () =>
        {
            RuleFor(x => x.PhysicalLimitations!)
                .Must(tags => tags.Count > 0)
                .WithMessage("PhysicalLimitations nao pode ser vazia.")
                .Must(tags => tags.All(t => AllowedPhysicalLimitationTags.Contains(t)))
                .WithMessage("PhysicalLimitations contem tags invalidas.");
        });

        When(x => x.PhysicalPains is not null, () =>
        {
            RuleFor(x => x.PhysicalPains!)
                .Must(tags => tags.Count > 0)
                .WithMessage("PhysicalPains nao pode ser vazia.")
                .Must(tags => tags.All(t => AllowedPhysicalPainTags.Contains(t)))
                .WithMessage("PhysicalPains contem tags invalidas.");
        });

        When(x => x.TrainingPreferences is not null, () =>
        {
            RuleFor(x => x.TrainingPreferences!)
                .Must(tags => tags.Count > 0)
                .WithMessage("TrainingPreferences nao pode ser vazia.")
                .Must(tags => tags.All(t => AllowedTrainingPreferenceTags.Contains(t)))
                .WithMessage("TrainingPreferences contem tags invalidas.");
        });
    }

    private static bool HasAtLeastOneField(UpdateUserProfileCommand command) =>
        command.Goal is not null ||
        command.ExperienceLevel is not null ||
        command.Age.HasValue ||
        command.HeightCm.HasValue ||
        command.WeightKg.HasValue ||
        command.BiologicalSex is not null ||
        command.TrainingDuration is not null ||
        command.TrainingLocation is not null ||
        command.EquipmentAvailable is not null ||
        command.AvailableMinutesPerWorkout.HasValue ||
        command.AvailableDaysPerWeek.HasValue ||
        command.BodyType is not null ||
        command.PhysicalLimitations is not null ||
        command.PhysicalPains is not null ||
        command.TrainingPreferences is not null;
}
