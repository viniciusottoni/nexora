using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Users;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Users.Queries.GetOnboardingStatus;

public class GetOnboardingStatusQueryHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusResponse>
{
    public async Task<OnboardingStatusResponse> Handle(
        GetOnboardingStatusQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUserService.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        var currentStep = user.IsOnboardingComplete
            ? "completed"
            : user.CurrentOnboardingStep ?? "goal";

        return new OnboardingStatusResponse(user.IsOnboardingComplete, currentStep);
    }
}
