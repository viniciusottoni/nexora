using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Users;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Onboarding.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler(
    IUserProfileRepository userProfileRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserProfileCommand, UserProfileResponse>
{
    public async Task<UserProfileResponse> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
            throw new NotFoundException("UserProfile", userId);

        profile.ApplyPatch(
            age: request.Age,
            heightCm: request.HeightCm,
            weightKg: request.WeightKg,
            biologicalSex: request.BiologicalSex,
            trainingDuration: request.TrainingDuration,
            trainingLocation: request.TrainingLocation,
            equipmentAvailable: request.EquipmentAvailable,
            availableMinutesPerWorkout: request.AvailableMinutesPerWorkout,
            availableDaysPerWeek: request.AvailableDaysPerWeek,
            bodyType: request.BodyType,
            physicalLimitations: request.PhysicalLimitations,
            physicalPains: request.PhysicalPains,
            trainingPreferences: request.TrainingPreferences,
            utcNow: dateTimeService.UtcNow,
            goal: request.Goal,
            experienceLevel: request.ExperienceLevel);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserProfileResponse(
            profile.Goal,
            profile.ExperienceLevel,
            profile.EffectiveExperienceLevel,
            profile.Age,
            profile.HeightCm,
            profile.WeightKg,
            profile.BiologicalSex,
            profile.TrainingDuration,
            profile.TrainingLocation,
            profile.EquipmentAvailable,
            profile.AvailableMinutesPerWorkout,
            profile.AvailableDaysPerWeek,
            profile.BodyType,
            profile.PhysicalLimitations,
            profile.PhysicalPains,
            profile.TrainingPreferences);
    }
}
