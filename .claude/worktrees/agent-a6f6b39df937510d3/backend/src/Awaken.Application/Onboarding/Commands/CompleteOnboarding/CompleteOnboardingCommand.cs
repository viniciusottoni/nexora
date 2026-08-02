using Awaken.Contracts.Onboarding;
using MediatR;

namespace Awaken.Application.Onboarding.Commands.CompleteOnboarding;

public record CompleteOnboardingCommand(
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
    List<string> PhysicalPains) : IRequest<CompleteOnboardingResponse>;
