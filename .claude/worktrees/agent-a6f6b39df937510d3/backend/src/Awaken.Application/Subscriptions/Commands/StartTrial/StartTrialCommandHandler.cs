using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Subscriptions.Commands.StartTrial;

public class StartTrialCommandHandler(
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<StartTrialCommandHandler> logger,
    IAuditLogService auditLogService,
    IAccessStatusCacheService accessStatusCache) : IRequestHandler<StartTrialCommand, StartTrialResponse>
{
    public async Task<StartTrialResponse> Handle(StartTrialCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var hasExistingSubscription = await subscriptionRepository.HasAnySubscriptionRecordAsync(userId, cancellationToken);
        if (hasExistingSubscription)
            throw new ConflictException("TRIAL_ALREADY_USED", "O período de teste gratuito já foi utilizado.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        var utcNow = dateTimeService.UtcNow;
        var trialEndsAt = utcNow.AddDays(7);

        var subscription = Subscription.CreateTrial(userId, utcNow, trialEndsAt);
        await subscriptionRepository.AddAsync(subscription, cancellationToken);

        user.StartTrial(trialEndsAt);

        await auditLogService.RecordAsync(
            "trial_started",
            userId,
            AuditActorType.User,
            "Subscription",
            subscription.Id,
            "{\"trialDays\":7}",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // US-205: invalidate cached access status so next request reflects new trial state.
        await accessStatusCache.InvalidateAsync(userId, cancellationToken);

        logger.LogInformation(
            "trial_started userId={UserId} trialEndsAt={TrialEndsAt}",
            userId, trialEndsAt);

        return new StartTrialResponse(
            user.ComputeAccessStatus(utcNow),
            utcNow,
            trialEndsAt);
    }
}
