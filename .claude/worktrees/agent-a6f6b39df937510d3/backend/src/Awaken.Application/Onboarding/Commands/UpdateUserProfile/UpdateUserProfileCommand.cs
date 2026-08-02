using Awaken.Contracts.Users;
using MediatR;

namespace Awaken.Application.Onboarding.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand(
    string? Goal,
    string? ExperienceLevel,
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
    List<string>? TrainingPreferences) : IRequest<UserProfileResponse>;
