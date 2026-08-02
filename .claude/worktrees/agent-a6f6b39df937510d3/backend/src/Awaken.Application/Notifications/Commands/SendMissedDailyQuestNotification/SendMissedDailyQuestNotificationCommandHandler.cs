using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.SendMissedDailyQuestNotification;

/// US-135: envia aviso após virada de dia quando a quest diária não foi concluída e a penalidade de XP foi aplicada.
/// RN-001: apenas usuários com PushEnabled=true e PushToken não nulo.
/// RN-002: apenas usuários com acesso ativo (trial_active ou subscription_active).
/// RN-003: enviar apenas se quest diária não foi completada.
/// RN-004: enviar apenas se a penalidade de XP foi aplicada (RecentDailyPenaltyXp > 0).
/// RN-005: tom encorajador, não punitivo.
/// RN-006: respeita consentimento e limite de notificações.
/// RN-007: não enviar se a quest foi concluída.
public class SendMissedDailyQuestNotificationCommandHandler(
    IQuestRepository questRepository,
    INotificationPreferenceRepository notificationPreferenceRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IHunterProgressionRepository hunterProgressionRepository,
    IPushNotificationService pushNotificationService,
    INotificationLogRepository notificationLogRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<SendMissedDailyQuestNotificationCommandHandler> logger)
    : IRequestHandler<SendMissedDailyQuestNotificationCommand, SendMissedDailyQuestNotificationResult>
{
    private const string NotificationType = "missed_daily_quest_notification";

    private static readonly Dictionary<string, string> PushData = new()
    {
        { "type", NotificationType },
        { "route", "/daily-quest" }
    };

    public async Task<SendMissedDailyQuestNotificationResult> Handle(
        SendMissedDailyQuestNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var today = dateTimeService.TodayUtc;
        var yesterdayUtc = DateTime.SpecifyKind(
            today.AddDays(-1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        // RN-003: quests diárias do dia anterior com penalidade verificada e status != completed.
        var missedQuests = await questRepository.GetMissedPenaltyCheckedByDateAsync(
            yesterdayUtc, cancellationToken);

        var eligible = 0;
        var sent = 0;
        var skipped = 0;

        foreach (var quest in missedQuests)
        {
            eligible++;

            // RN-006: verificar consentimento e limite diário.
            var preference = await notificationPreferenceRepository.GetByUserIdAsync(quest.UserId, cancellationToken);
            if (preference is null || !preference.CanReceiveNotificationToday(today))
            {
                await LogDecisionAsync(preference?.UserId ?? quest.UserId, "ignored", "daily_limit_reached", utcNow, cancellationToken);
                logger.LogInformation("notification_send_blocked_by_limit userId={UserId} type={Type}", quest.UserId, NotificationType);
                skipped++;
                continue;
            }

            var user = await userRepository.GetByIdAsync(quest.UserId, cancellationToken);
            if (user is null)
            {
                skipped++;
                continue;
            }

            // RN-002: acesso ativo (trial ou assinatura).
            var subscription = await subscriptionRepository.GetByUserIdAsync(quest.UserId, cancellationToken);
            var accessStatus = subscription?.Plan is "monthly" or "annual"
                ? subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired"
                : user.ComputeAccessStatus(utcNow);

            if (accessStatus is not ("trial_active" or "subscription_active"))
            {
                await LogDecisionAsync(quest.UserId, "ignored", "inactive_access", utcNow, cancellationToken);
                skipped++;
                continue;
            }

            // RN-004: penalidade de XP foi aplicada.
            var progression = await hunterProgressionRepository.GetByUserIdAsync(quest.UserId, cancellationToken);
            if (progression is null || progression.RecentDailyPenaltyXp is null or 0)
            {
                await LogDecisionAsync(quest.UserId, "ignored", "no_penalty_applied", utcNow, cancellationToken);
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
                await LogDecisionAsync(quest.UserId, "sent", null, utcNow, cancellationToken);

                // ADR-015: sem dados pessoais ou tokens nos logs.
                logger.LogInformation(
                    "missed_daily_quest_notification_sent userId={UserId} penaltyXp={PenaltyXp}",
                    quest.UserId,
                    progression.RecentDailyPenaltyXp);

                sent++;
            }
            catch (Exception ex)
            {
                await LogDecisionAsync(quest.UserId, "failed", "push_send_failed", utcNow, cancellationToken);
                logger.LogWarning(ex, "notification_send_failed userId={UserId} type={Type}", quest.UserId, NotificationType);
                skipped++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SendMissedDailyQuestNotificationResult(eligible, sent, skipped);
    }

    private async Task LogDecisionAsync(
        Guid userId,
        string status,
        string? reason,
        DateTime utcNow,
        CancellationToken ct)
    {
        var log = NotificationLog.Create(userId, NotificationType, status, reason, utcNow);
        await notificationLogRepository.AddAsync(log, ct);
    }

    // RN-005: tom encorajador, nunca punitivo.
    private static (string Title, string Body) GetLocalizedContent(string preferredLanguage) =>
        preferredLanguage switch
        {
            "en" => ("You missed yesterday's quest", "No worries — a new quest awaits you today. Keep going, Hunter!"),
            "es" => ("Perdiste la quest de ayer", "No te preocupes — hoy tienes una nueva oportunidad. ¡Sigue adelante, Hunter!"),
            "fr" => ("Tu as raté la quête d'hier", "Pas de souci — une nouvelle quête t'attend aujourd'hui. Continue, Hunter!"),
            _ => ("Você perdeu a quest de ontem", "Tudo bem — uma nova quest te espera hoje. Continue sua jornada, Hunter!")
        };
}
