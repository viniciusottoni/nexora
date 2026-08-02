using MediatR;

namespace Awaken.Application.Onboarding.Commands.SavePhysicalData;

public record SavePhysicalDataCommand(
    int? Age,
    decimal? HeightCm,
    decimal? WeightKg,
    string? BiologicalSex,
    string? TrainingDuration,
    int? AvailableMinutesPerWorkout,
    string? BodyType,
    List<string>? PhysicalLimitations,
    List<string>? PhysicalPains) : IRequest<Unit>;
