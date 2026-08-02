using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Users;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler(
    IUserProfileRepository userProfileRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetUserProfileQuery, UserProfileResponse>
{
    public async Task<UserProfileResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
            throw new NotFoundException("UserProfile", userId);

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
