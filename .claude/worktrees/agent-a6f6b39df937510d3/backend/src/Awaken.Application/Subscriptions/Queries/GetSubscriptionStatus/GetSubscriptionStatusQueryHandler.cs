using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Subscriptions.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusQueryHandler(
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<GetSubscriptionStatusQueryHandler> logger) : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusResponse>
{
    public async Task<SubscriptionStatusResponse> Handle(
        GetSubscriptionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        var utcNow = dateTimeService.UtcNow;
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);

        // Paid plan takes priority over trial
        if (subscription?.Plan is "monthly" or "annual")
        {
            var isActive = subscription.ExpiresAt > utcNow;
            var status = isActive ? "subscription_active" : "subscription_expired";

            if (!isActive && subscription.MarkExpired(utcNow))
                await unitOfWork.SaveChangesAsync(cancellationToken);

            int? daysRemaining = null;
            if (isActive && subscription.ExpiresAt is not null)
            {
                var remaining = (subscription.ExpiresAt.Value - utcNow).TotalDays;
                daysRemaining = (int)Math.Ceiling(remaining);
                if (daysRemaining < 0) daysRemaining = 0;
            }

            return new SubscriptionStatusResponse(
                status, null, null, daysRemaining, subscription.Plan, subscription.ExpiresAt);
        }

        // Trial logic
        var accessStatus = user.ComputeAccessStatus(utcNow);

        if (accessStatus == "trial_expired" && subscription is not null)
        {
            if (subscription.ExpireTrial())
            {
                logger.LogInformation(
                    "trial_expired userId={UserId}", userId);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        int? trialDaysRemaining = null;
        if (subscription?.TrialEndsAt is not null && accessStatus == "trial_active")
        {
            var remaining = (subscription.TrialEndsAt.Value - utcNow).TotalDays;
            trialDaysRemaining = (int)Math.Ceiling(remaining);
            if (trialDaysRemaining < 0) trialDaysRemaining = 0;
        }

        return new SubscriptionStatusResponse(
            accessStatus,
            subscription?.TrialStartedAt,
            subscription?.TrialEndsAt,
            trialDaysRemaining);
    }
}
