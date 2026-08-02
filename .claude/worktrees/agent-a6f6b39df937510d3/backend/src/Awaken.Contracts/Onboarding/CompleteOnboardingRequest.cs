namespace Awaken.Contracts.Onboarding;

public record CompleteOnboardingRequest(
    string Goal,
    string ExperienceLevel,
    int Age,
    decimal HeightCm,
    decimal WeightKg,
    string BiologicalSex,
    string TrainingDuration,
    int AvailableMinutesPerWorkout,
    string BodyType,
    List<string> PhysicalLimitations,
    List<string> PhysicalPains);
