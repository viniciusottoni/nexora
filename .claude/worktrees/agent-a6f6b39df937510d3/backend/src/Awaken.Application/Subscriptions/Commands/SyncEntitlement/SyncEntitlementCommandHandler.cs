using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Subscriptions;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using Awaken.Shared.Audit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Subscriptions.Commands.SyncEntitlement;

/// <summary>
/// US-194: status sync triggered by the app.
///
/// Subscription activation sources (in order of priority):
///   1. RevenueCat server-side API validation (production path).
///   2. Client-supplied hints (ClientPlan / ClientEntitlement / ClientExpiresAt)
///      when RevenueCat:DevModeEnabled = true — sandbox / Test Store only.
///
/// The app never supplies plan or expiry as authority in production (ADR-009).
/// </summary>
public class SyncEntitlementCommandHandler(
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    ICurrentUserService currentUserService,
    IAccessStatusCacheService accessStatusCache,
    IRevenueCatValidationService revenueCatValidationService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<SyncEntitlementCommandHandler> logger,
    IAuditLogService auditLogService,
    IConfiguration configuration) : IRequestHandler<SyncEntitlementCommand, SyncEntitlementResponse>
{
    public async Task<SyncEntitlementResponse> Handle(
        SyncEntitlementCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        _ = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        var utcNow = dateTimeService.UtcNow;
        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);

        // Track whether access was previously blocked (for the accessRestored flag).
        var wasAccessBlocked = subscription is not null && WasSubscriptionAccessBlocked(subscription, utcNow);

        var hasChanges = false;
        if (!string.IsNullOrEmpty(request.RevenueCatCustomerId))
        {
            var validation = await revenueCatValidationService.ValidateSubscriptionAsync(
                request.RevenueCatCustomerId,
                request.OriginalRevenueCatCustomerId,
                utcNow,
                cancellationToken);

            logger.LogDebug(
                "subscription_sync_validation userId={UserId} revenueCatCustomerId={RevenueCatCustomerId} isActive={IsActive} plan={Plan} entitlement={Entitlement} productId={ProductId} expiresAt={ExpiresAt}",
                userId,
                request.RevenueCatCustomerId,
                validation.IsActive,
                validation.Plan,
                validation.Entitlement,
                validation.ProductId,
                validation.ExpiresAtUtc);

            if (!validation.IsActive)
            {
                validation = TryApplyDevModeClientHints(request, utcNow, userId, validation);
            }

            if (validation.IsActive &&
                validation.Plan is "monthly" or "annual" &&
                validation.ExpiresAtUtc.HasValue)
            {
                var entitlement = validation.Entitlement ?? "revenuecat";
                if (subscription is null)
                {
                    subscription = Awaken.Domain.Entities.Subscriptions.Subscription.CreateFromPaidPlan(
                        userId,
                        validation.Plan,
                        entitlement,
                        request.RevenueCatCustomerId,
                        validation.ExpiresAtUtc.Value,
                        utcNow);
                    await subscriptionRepository.AddAsync(subscription, cancellationToken);
                }
                else
                {
                    subscription.ActivatePaidPlan(
                        validation.Plan,
                        entitlement,
                        request.RevenueCatCustomerId,
                        validation.ExpiresAtUtc.Value,
                        utcNow);
                    subscriptionRepository.Update(subscription);
                }

                hasChanges = true;
            }
            else if (subscription is not null &&
                string.IsNullOrEmpty(subscription.RevenueCatCustomerId))
            {
                // Keep webhook correlation even when the direct lookup is not active yet.
                subscription.LinkRevenueCatCustomerId(request.RevenueCatCustomerId, utcNow);
                subscriptionRepository.Update(subscription);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // US-205: invalidate cached access status so next request is fresh.
        await accessStatusCache.InvalidateAsync(userId, cancellationToken);

        // Derive server-authoritative access status.
        string accessStatus;
        string? plan = null;
        DateTime? expiresAt = null;

        if (subscription is null)
        {
            accessStatus = "no_subscription";
        }
        else if (subscription.Plan is "monthly" or "annual")
        {
            var isActive = subscription.ExpiresAt > utcNow;
            accessStatus = isActive ? "subscription_active" : "subscription_expired";
            plan = subscription.Plan;
            expiresAt = subscription.ExpiresAt;
        }
        else
        {
            // Trial or other plan.
            var trialActive = subscription.Status is "trial_active" &&
                (subscription.TrialEndsAt is null || subscription.TrialEndsAt > utcNow);
            accessStatus = trialActive ? "trial_active" : "trial_expired";
        }

        var accessRestored = accessStatus == "subscription_active" && wasAccessBlocked;

        try
        {
            await auditLogService.RecordAsync(
                "subscription_synced",
                userId,
                AuditActorType.User,
                "Subscription",
                subscription?.Id,
                AuditMetadata.Safe(new { accessStatus, plan }),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write audit log for subscription_synced userId={UserId}", userId);
        }

        logger.LogInformation(
            "subscription_synced userId={UserId} accessStatus={AccessStatus} accessRestored={AccessRestored}",
            userId, accessStatus, accessRestored);

        return new SyncEntitlementResponse(accessStatus, plan, expiresAt, accessRestored);
    }

    /// <summary>
    /// Dev-only: when RevenueCat:DevModeEnabled = true and RC validation returned
    /// inactive (e.g. Test Store / sandbox without real keys), trust the client-supplied
    /// plan/entitlement/expiresAt so the full subscription flow can be exercised locally.
    /// Never called in production.
    /// </summary>
    private RevenueCatSubscriptionValidation TryApplyDevModeClientHints(
        SyncEntitlementCommand request,
        DateTime utcNow,
        Guid userId,
        RevenueCatSubscriptionValidation original)
    {
        var devMode = string.Equals(configuration["RevenueCat:DevModeEnabled"], "true", StringComparison.OrdinalIgnoreCase);
        if (!devMode) return original;

        var plan = request.ClientPlan;
        var entitlement = request.ClientEntitlement;
        var expiresAt = request.ClientExpiresAt;

        if (plan is not ("monthly" or "annual") ||
            string.IsNullOrEmpty(entitlement) ||
            expiresAt is null ||
            expiresAt <= utcNow)
        {
            return original;
        }

        logger.LogWarning(
            "subscription_sync_dev_override userId={UserId} plan={Plan} entitlement={Entitlement} expiresAt={ExpiresAt} — RC validation bypassed (DevModeEnabled=true)",
            userId, plan, entitlement, expiresAt);

        return new RevenueCatSubscriptionValidation(true, plan, entitlement, null, expiresAt);
    }

    private static bool WasSubscriptionAccessBlocked(
        Awaken.Domain.Entities.Subscriptions.Subscription subscription, DateTime utcNow)
    {
        if (subscription.Plan is "monthly" or "annual")
        {
            return subscription.Status == "subscription_expired" ||
                subscription.ExpiresAt <= utcNow;
        }

        return subscription.Status == "trial_expired" ||
            subscription.TrialEndsAt <= utcNow;
    }
}
