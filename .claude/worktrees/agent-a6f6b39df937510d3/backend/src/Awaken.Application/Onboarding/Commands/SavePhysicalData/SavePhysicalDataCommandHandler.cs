using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Onboarding.Commands.SavePhysicalData;

public class SavePhysicalDataCommandHandler(
    IUserProfileRepository userProfileRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<SavePhysicalDataCommand, Unit>
{
    public async Task<Unit> Handle(SavePhysicalDataCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var utcNow = dateTimeService.UtcNow;

        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            profile = UserProfile.Create(
                userId: userId,
                age: request.Age,
                heightCm: request.HeightCm,
                weightKg: request.WeightKg,
                biologicalSex: request.BiologicalSex,
                trainingDuration: request.TrainingDuration,
                availableMinutesPerWorkout: request.AvailableMinutesPerWorkout,
                bodyType: request.BodyType,
                physicalLimitations: request.PhysicalLimitations,
                physicalPains: request.PhysicalPains);
            await userProfileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.ApplyPatch(
                age: request.Age,
                heightCm: request.HeightCm,
                weightKg: request.WeightKg,
                biologicalSex: request.BiologicalSex,
                trainingDuration: request.TrainingDuration,
                trainingLocation: null,
                equipmentAvailable: null,
                availableMinutesPerWorkout: request.AvailableMinutesPerWorkout,
                availableDaysPerWeek: null,
                bodyType: request.BodyType,
                physicalLimitations: request.PhysicalLimitations,
                physicalPains: request.PhysicalPains,
                trainingPreferences: null,
                utcNow: utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
