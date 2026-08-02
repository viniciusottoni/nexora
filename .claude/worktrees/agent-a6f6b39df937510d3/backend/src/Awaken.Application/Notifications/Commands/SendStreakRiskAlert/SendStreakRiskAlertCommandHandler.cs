using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.SendStreakRiskAlert;

/// US-093: envia alerta de streak em risco para usuarios com streak ativo que nao completaram
/// a quest diaria ainda.
/// US-095: registra cada decisão de envio no NotificationLog (RN-007).
/// RN-001: apenas usuarios com PushEnabled=true e PushToken nao nulo.
/// RN-002: apenas usuarios com acesso ativo (trial_active ou subscription_active).
/// RN-003: apenas usuarios com CurrentStreakDays > 0.
/// RN-004: nao envia se a quest do dia ja foi completada.
/// RN-005: respeita limite de 3 notificacoes por dia por usuario.
public class SendStreakRiskAlertCommandHandler(
    INotificationPreferenceRepository notificationPreferenceRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IQuestRepository questRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IPushNotificationService pushNotificationService,
    INotificationLogRepository notificationLogRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<SendStreakRiskAlertCommandHandler> logger)
    : IRequestHandler<SendStreakRiskAlertCommand, SendStreakRiskAlertResult>
{
    private const string NotificationType = "streak_risk_alert";

    private static readonly Dictionary<string, string> PushData = new()
    {
        { "type", NotificationType },
        { "route", "/daily-quest" }
    };

    public async Task<SendStreakRiskAlertResult> Handle(
        SendStreakRiskAlertCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var today = dateTimeService.TodayUtc;
        var todayUtc = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var preferences = await notificationPreferenceRepository
            .GetAllWithPushEnabledAsync(cancellationToken);

        var eligible = 0;
        var sent = 0;
        var skipped = 0;

        foreach (var preference in preferences)
        {
            eligible++;

            // RN-001/RN-005: push habilitado, token presente e limite diario nao atingido.
            if (!preference.CanReceiveNotificationToday(today))
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "daily_limit_reached", utcNow, cancellationToken);
                logger.LogInformation("notification_send_blocked_by_limit userId={UserId} type={Type}", preference.UserId, NotificationType);
                skipped++;
                continue;
            }

            var user = await userRepository.GetByIdAsync(preference.UserId, cancellationToken);
            if (user is null)
            {
                skipped++;
                continue;
            }

            // RN-002: acesso ativo (trial ou assinatura).
            var subscription = await subscriptionRepository.GetByUserIdAsync(preference.UserId, cancellationToken);
            var accessStatus = subscription?.Plan is "monthly" or "annual"
                ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
                : user.ComputeAccessStatus(utcNow);

            if (accessStatus is not ("trial_active" or "subscription_active"))
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "inactive_access", utcNow, cancellationToken);
                skipped++;
                continue;
            }

            // RN-003: usuario deve ter streak ativo (CurrentStreakDays > 0).
            var progression = await hunterProgressionRepository.GetByUserIdAsync(preference.UserId, cancellationToken);
            if (progression is null || progression.CurrentStreakDays == 0)
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "no_streak", utcNow, cancellationToken);
                skipped++;
                continue;
            }

            // RN-004: nao envia se a quest do dia ja foi completada.
            var quest = await questRepository.GetByUserIdAndDateAsync(
                preference.UserId, "daily", todayUtc, cancellationToken);

            if (quest?.Status == "completed")
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "quest_completed", utcNow, cancellationToken);
                skipped++;
                continue;
            }

            var (title, body) = GetLocalizedContent(user.PreferredLanguage);

            try
            {
                await pushNotificationService.SendAsync(
                    preference.PushToken!,
                    title,
                    body,
                    PushData,
                    cancellationToken);

                preference.RecordNotificationSent(utcNow);
                notificationPreferenceRepository.Update(preference);
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "sent", null, utcNow, cancellationToken);

                // ADR-015: sem dados pessoais ou tokens nos logs.
                logger.LogInformation("notification_send_decision_logged userId={UserId} type={Type} status=sent", preference.UserId, NotificationType);
                logger.LogInformation(
                    "streak_risk_notification_sent userId={UserId} streakDays={Days}",
                    preference.UserId,
                    progression.CurrentStreakDays);

                sent++;
            }
            catch (Exception ex)
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "failed", "push_send_failed", utcNow, cancellationToken);
                logger.LogWarning(ex, "notification_send_failed userId={UserId} type={Type}", preference.UserId, NotificationType);
                skipped++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SendStreakRiskAlertResult(eligible, sent, skipped);
    }

    private static async Task LogDecisionAsync(
        INotificationLogRepository repo,
        Guid userId,
        string status,
        string? reason,
        DateTime utcNow,
        CancellationToken ct)
    {
        var log = NotificationLog.Create(userId, NotificationType, status, reason, utcNow);
        await repo.AddAsync(log, ct);
    }

    private static (string Title, string Body) GetLocalizedContent(string preferredLanguage) =>
        preferredLanguage switch
        {
            "en" => ("Your streak is at risk!", "Complete your quest today to keep your streak alive."),
            "es" => ("¡Tu racha está en riesgo!", "Completa tu quest hoy para mantener la racha."),
            "fr" => ("Ta série est en danger!", "Complète ta quête aujourd'hui pour garder ta série."),
            _ => ("Seu streak está em risco!", "Complete sua quest hoje para manter a sequência.")
        };
}
