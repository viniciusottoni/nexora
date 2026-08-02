using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Users;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Users.Queries.GetEffectiveExperienceLevel;

public class GetEffectiveExperienceLevelQueryHandler(
    IUserProfileRepository userProfileRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetEffectiveExperienceLevelQuery, EffectiveExperienceLevelResponse>
{
    public async Task<EffectiveExperienceLevelResponse> Handle(
        GetEffectiveExperienceLevelQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var profile = await userProfileRepository.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
            throw new NotFoundException("UserProfile", userId);

        return new EffectiveExperienceLevelResponse(
            profile.ExperienceLevel,
            profile.TrainingDuration,
            profile.EffectiveExperienceLevel);
    }
}
