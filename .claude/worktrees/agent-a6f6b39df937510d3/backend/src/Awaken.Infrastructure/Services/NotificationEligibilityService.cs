using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Repositories;

namespace Awaken.Infrastructure.Services;

/// US-095: serviço central de elegibilidade de notificação.
/// RN-005: verifica consentimento (push habilitado + token presente).
/// RN-002: verifica acesso ativo (trial ou assinatura).
/// RN-006: usuário com acesso ativo não recebe reactivation.
/// RN-003/RN-004: tipos HIGH-priority (streak_risk_alert, trial_expiring) ignoram limite diário.
public class NotificationEligibilityService(
    INotificationPreferenceRepository notificationPreferenceRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    INotificationLogRepository notificationLogRepository)
    : INotificationEligibilityService
{
    private static readonly HashSet<string> HighPriorityTypes =
        ["streak_risk_alert", "trial_expiring"];

    public async Task<EligibilityResult> EvaluateAsync(
        Guid userId,
        string notificationType,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(utcNow);

        // RN-005: consentimento.
        var preference = await notificationPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        if (preference is null || !preference.PushEnabled || preference.PushToken is null)
            return EligibilityResult.Blocked("no_consent");

        // RN-002 / RN-006: verificação de acesso baseada no tipo.
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return EligibilityResult.Blocked("user_not_found");

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
        var accessStatus = subscription?.Plan is "monthly" or "annual"
            ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
            : user.ComputeAccessStatus(utcNow);

        bool hasActiveAccess = accessStatus is "trial_active" or "subscription_active";

        if (notificationType == "reactivation")
        {
            if (hasActiveAccess)
                return EligibilityResult.Blocked("active_access_for_reactivation");
        }
        else
        {
            if (!hasActiveAccess)
                return EligibilityResult.Blocked("inactive_access");
        }

        // Redundância: mesmo tipo já enviado hoje.
        var todayLogs = await notificationLogRepository.GetTodayByUserIdAsync(userId, today, cancellationToken);
        if (todayLogs.Any(l => l.NotificationType == notificationType && l.DecisionStatus == "sent"))
            return EligibilityResult.Blocked("redundant");

        var currentPriority = GetPriority(notificationType);
        if (todayLogs.Any(l => l.DecisionStatus == "sent" && GetPriority(l.NotificationType) >= currentPriority))
            return EligibilityResult.Blocked("higher_priority_already_sent");

        // RN-002/RN-003/RN-004: limite diário — HIGH-priority ignora limite.
        bool isHighPriority = HighPriorityTypes.Contains(notificationType);
        if (!isHighPriority && !preference.CanReceiveNotificationToday(today))
            return EligibilityResult.Blocked("daily_limit_reached");

        return EligibilityResult.Allow();
    }

    private static int GetPriority(string notificationType) =>
        notificationType switch
        {
            "streak_risk_alert" => 3,
            "trial_expiring" => 2,
            "daily_quest_reminder" => 1,
            "reactivation" => 0,
            _ => 0
        };
}
