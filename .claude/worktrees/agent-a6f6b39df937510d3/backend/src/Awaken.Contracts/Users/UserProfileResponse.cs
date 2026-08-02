namespace Awaken.Contracts.Users;

public record UserProfileResponse(
    string? Goal,
    string? ExperienceLevel,
    string? EffectiveExperienceLevel,
    int? Age,
    decimal? HeightCm,
    decimal? WeightKg,
    string? BiologicalSex,
    string? TrainingDuration,
    string? TrainingLocation,
    List<string>? EquipmentAvailable,
    int? AvailableMinutesPerWorkout,
    int? AvailableDaysPerWeek,
    string? BodyType,
    List<string>? PhysicalLimitations,
    List<string>? PhysicalPains,
    List<string>? TrainingPreferences);
