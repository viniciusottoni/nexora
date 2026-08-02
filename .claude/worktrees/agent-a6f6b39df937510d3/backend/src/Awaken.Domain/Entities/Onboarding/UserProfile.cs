using Awaken.Domain.Common;
using Awaken.Domain.Services.Progression;

namespace Awaken.Domain.Entities.Onboarding;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string? Goal { get; private set; }
    public string? ExperienceLevel { get; private set; }
    public string? EffectiveExperienceLevel { get; private set; }
    public int? Age { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? WeightKg { get; private set; }
    public string? BiologicalSex { get; private set; }
    public string? TrainingDuration { get; private set; }
    public string? TrainingLocation { get; private set; }
    public List<string>? EquipmentAvailable { get; private set; }
    public int? AvailableMinutesPerWorkout { get; private set; }
    public int? AvailableDaysPerWeek { get; private set; }
    public string? BodyType { get; private set; }
    public List<string>? PhysicalLimitations { get; private set; }
    public List<string>? PhysicalPains { get; private set; }
    public List<string>? TrainingPreferences { get; private set; }

    private UserProfile() { }

    public static UserProfile Create(
        Guid userId,
        string? goal = null,
        string? experienceLevel = null,
        int? age = null,
        decimal? heightCm = null,
        decimal? weightKg = null,
        string? biologicalSex = null,
        string? trainingDuration = null,
        string? trainingLocation = null,
        List<string>? equipmentAvailable = null,
        int? availableMinutesPerWorkout = null,
        int? availableDaysPerWeek = null,
        string? bodyType = null,
        List<string>? physicalLimitations = null,
        List<string>? physicalPains = null,
        List<string>? trainingPreferences = null)
    {
        return new UserProfile
        {
            UserId = userId,
            Goal = goal,
            ExperienceLevel = experienceLevel,
            EffectiveExperienceLevel = ExperienceLevelCalculator.CalculateEffectiveLevel(experienceLevel, trainingDuration),
            Age = age,
            HeightCm = heightCm,
            WeightKg = weightKg,
            BiologicalSex = Normalize(biologicalSex),
            TrainingDuration = trainingDuration,
            TrainingLocation = trainingLocation,
            EquipmentAvailable = equipmentAvailable,
            AvailableMinutesPerWorkout = availableMinutesPerWorkout,
            AvailableDaysPerWeek = availableDaysPerWeek,
            BodyType = bodyType,
            PhysicalLimitations = physicalLimitations,
            PhysicalPains = physicalPains,
            TrainingPreferences = trainingPreferences,
        };
    }

    public void ApplyPatch(
        int? age,
        decimal? heightCm,
        decimal? weightKg,
        string? biologicalSex,
        string? trainingDuration,
        string? trainingLocation,
        List<string>? equipmentAvailable,
        int? availableMinutesPerWorkout,
        int? availableDaysPerWeek,
        string? bodyType,
        List<string>? physicalLimitations,
        List<string>? physicalPains,
        List<string>? trainingPreferences,
        DateTime utcNow,
        string? goal = null,
        string? experienceLevel = null)
    {
        if (goal is not null) Goal = goal;
        if (experienceLevel is not null) ExperienceLevel = experienceLevel;
        if (age.HasValue) Age = age.Value;
        if (heightCm.HasValue) HeightCm = heightCm.Value;
        if (weightKg.HasValue) WeightKg = weightKg.Value;
        if (biologicalSex is not null) BiologicalSex = Normalize(biologicalSex);
        if (trainingDuration is not null) TrainingDuration = trainingDuration;
        if (trainingLocation is not null) TrainingLocation = trainingLocation;
        if (equipmentAvailable is not null) EquipmentAvailable = equipmentAvailable;
        if (availableMinutesPerWorkout.HasValue) AvailableMinutesPerWorkout = availableMinutesPerWorkout.Value;
        if (availableDaysPerWeek.HasValue) AvailableDaysPerWeek = availableDaysPerWeek.Value;
        if (bodyType is not null) BodyType = bodyType;
        if (physicalLimitations is not null) PhysicalLimitations = physicalLimitations;
        if (physicalPains is not null) PhysicalPains = physicalPains;
        if (trainingPreferences is not null) TrainingPreferences = trainingPreferences;
        if (experienceLevel is not null || trainingDuration is not null)
            EffectiveExperienceLevel = ExperienceLevelCalculator.CalculateEffectiveLevel(ExperienceLevel, TrainingDuration);
        UpdatedAtUtc = utcNow;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return value.Trim();
    }
}
