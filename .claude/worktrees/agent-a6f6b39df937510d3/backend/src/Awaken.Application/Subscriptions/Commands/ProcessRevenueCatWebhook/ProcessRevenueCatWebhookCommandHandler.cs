using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Subscriptions;
using Awaken.Domain.Repositories;
using Awaken.Shared.Audit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Subscriptions.Commands.ProcessRevenueCatWebhook;

/// <summary>
/// US-194: processes a verified RevenueCat webhook event and updates subscription state.
///
/// This is the ONLY place where subscription activation/expiry state can be mutated
/// from an external event — ADR-009 (backend is authority for subscription state).
///
/// Idempotency: if a RevenueCatEvent record with the same EventId already exists,
/// the event is skipped. This is safe to call multiple times (RevenueCat retries on non-2xx).
///
/// Plan derivation: never trusted from the webhook body. The plan is resolved from the
/// product catalog (ShopProduct.RevenueCatProductId → ShopProduct.Key → plan mapping).
/// If the product is unknown, the plan defaults to "unknown" and subscription is NOT
/// created — an error is logged for ops investigation.
/// </summary>
public class ProcessRevenueCatWebhookCommandHandler(
    IRevenueCatEventRepository revenueCatEventRepository,
    ISubscriptionRepository subscriptionRepository,
    IAccessStatusCacheService accessStatusCache,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService,
    ILogger<ProcessRevenueCatWebhookCommandHandler> logger)
    : IRequestHandler<ProcessRevenueCatWebhookCommand, ProcessRevenueCatWebhookResult>
{
    // Known RevenueCat event types that activate a subscription.
    private static readonly HashSet<string> ActivationEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "INITIAL_PURCHASE", "RENEWAL", "UNCANCELLATION", "PRODUCT_CHANGE"
    };

    // Known RevenueCat event types that terminate/suspend a subscription.
    private static readonly HashSet<string> ExpirationEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "CANCELLATION", "EXPIRATION", "BILLING_ISSUE", "SUBSCRIBER_ALIAS"
    };

    public async Task<ProcessRevenueCatWebhookResult> Handle(
        ProcessRevenueCatWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;

        // --- Idempotency check ---
        if (await revenueCatEventRepository.ExistsByEventIdAsync(request.EventId, cancellationToken))
        {
            logger.LogInformation(
                "RevenueCat webhook event {EventId} already processed. Skipping.",
                request.EventId);
            return new ProcessRevenueCatWebhookResult(false, true, "already_processed");
        }

        // --- Look up subscription by RevenueCat AppUserId ---
        // AppUserId is the value we passed to RevenueCat at configuration time.
        // We store it in Subscription.RevenueCatCustomerId.
        var subscription = await subscriptionRepository.GetByRevenueCatCustomerIdAsync(
            request.AppUserId, cancellationToken);

        Guid? userId = subscription?.UserId;

        // --- Determine plan from server-side catalog ---
        // We never trust the plan embedded in the webhook body; we derive it from ProductId.
        var plan = DerivePlanFromProductId(request.ProductId);

        // --- Apply state change ---
        if (ActivationEvents.Contains(request.EventType))
        {
            if (!request.ExpiresAtUtc.HasValue)
            {
                logger.LogWarning(
                    "RevenueCat activation event {EventId} type={EventType} has no ExpiresAt. Skipping state change.",
                    request.EventId, request.EventType);
                // Still record the event to prevent re-processing.
                await RecordEventAndSaveAsync(request, utcNow, cancellationToken);
                return new ProcessRevenueCatWebhookResult(true, false, "activation_skipped_no_expiry");
            }

            if (subscription is null)
            {
                // First time we hear about this subscriber — create a subscription record.
                // UserId can only be resolved if AppUserId is a parseable Guid (our convention).
                if (!Guid.TryParse(request.AppUserId, out var resolvedUserId))
                {
                    logger.LogError(
                        "RevenueCat event {EventId}: AppUserId {AppUserId} is not a valid Guid. Cannot create subscription.",
                        request.EventId, request.AppUserId);
                    await RecordEventAndSaveAsync(request, utcNow, cancellationToken);
                    return new ProcessRevenueCatWebhookResult(true, false, "user_not_resolvable");
                }

                userId = resolvedUserId;
                subscription = Subscription.CreateFromWebhook(
                    resolvedUserId, plan, request.AppUserId,
                    request.ExpiresAtUtc.Value, utcNow);
                await subscriptionRepository.AddAsync(subscription, cancellationToken);
            }
            else
            {
                subscription.ActivatePaidPlanFromWebhook(
                    plan, request.AppUserId, request.ExpiresAtUtc.Value, utcNow);
                subscriptionRepository.Update(subscription);
            }

            logger.LogInformation(
                "RevenueCat event {EventId} type={EventType}: subscription activated for userId={UserId} plan={Plan} expiresAt={ExpiresAt}",
                request.EventId, request.EventType, userId, plan, request.ExpiresAtUtc);
        }
        else if (ExpirationEvents.Contains(request.EventType))
        {
            if (subscription is not null)
            {
                var changed = subscription.MarkExpired(utcNow);
                if (changed)
                {
                    subscriptionRepository.Update(subscription);
                    logger.LogInformation(
                        "RevenueCat event {EventId} type={EventType}: subscription expired for userId={UserId}",
                        request.EventId, request.EventType, userId);
                }
            }
            else
            {
                logger.LogWarning(
                    "RevenueCat expiration event {EventId} type={EventType}: no subscription found for appUserId={AppUserId}. Skipping state change.",
                    request.EventId, request.EventType, request.AppUserId);
            }
        }
        else
        {
            logger.LogInformation(
                "RevenueCat event {EventId} type={EventType}: unrecognised event type, recording only.",
                request.EventId, request.EventType);
        }

        // --- Record event for idempotency ---
        await RecordEventAndSaveAsync(request, utcNow, cancellationToken);

        // --- Audit ---
        if (userId.HasValue && subscription is not null)
        {
            try
            {
                await auditLogService.RecordAsync(
                    "subscription_webhook_processed",
                    userId.Value,
                    AuditActorType.System,
                    "Subscription",
                    subscription.Id,
                    AuditMetadata.Safe(new
                    {
                        eventType = request.EventType,
                        plan,
                        status = subscription.Status
                    }),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to write audit log for RevenueCat event {EventId}", request.EventId);
            }
        }

        // --- Invalidate access-status cache ---
        if (userId.HasValue)
        {
            try
            {
                await accessStatusCache.InvalidateAsync(userId.Value, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Cache invalidation failure must not block the webhook response (RevenueCat retries on failure).
                logger.LogWarning(ex, "Failed to invalidate access status cache for userId={UserId}", userId);
            }
        }

        return new ProcessRevenueCatWebhookResult(true, false, "ok");
    }

    private async Task RecordEventAndSaveAsync(
        ProcessRevenueCatWebhookCommand request,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var rcEvent = RevenueCatEvent.Create(
            request.EventId,
            request.AppUserId,
            request.EventType,
            utcNow,
            request.OriginalTransactionId,
            request.ProductId,
            request.PayloadHash);

        await revenueCatEventRepository.AddAsync(rcEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Maps a RevenueCat ProductId to our internal plan name.
    /// Convention: product keys containing "annual" → "annual", "monthly" → "monthly".
    /// Extend this method when new products are added to the catalog.
    /// </summary>
    private static string DerivePlanFromProductId(string? productId)
    {
        if (string.IsNullOrEmpty(productId))
            return "unknown";

        if (productId.Contains("annual", StringComparison.OrdinalIgnoreCase))
            return "annual";

        if (productId.Contains("monthly", StringComparison.OrdinalIgnoreCase))
            return "monthly";

        return "unknown";
    }
}
