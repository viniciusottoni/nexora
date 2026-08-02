using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Auth.Queries.GetSession;

public class GetSessionQueryHandler(
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService) : IRequestHandler<GetSessionQuery, SessionResponse>
{
    public async Task<SessionResponse> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUserService.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        var utcNow = dateTimeService.UtcNow;
        var subscription = await subscriptionRepository.GetByUserIdAsync(user.Id, cancellationToken);
        var accessStatus = subscription?.Plan is "monthly" or "annual"
            ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
            : user.ComputeAccessStatus(utcNow);

        return new SessionResponse(
            user.Id,
            true,
            accessStatus,
            user.IsOnboardingComplete);
    }
}
