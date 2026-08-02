using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.SendDailyQuestReminder;

/// US-092: envia lembretes de quest diaria para usuarios elegiveis via push notification.
/// US-095: registra cada decisão de envio no NotificationLog (RN-007).
/// RN-001: apenas usuarios com PushEnabled=true e PushToken nao nulo.
/// RN-002: apenas usuarios com acesso ativo (trial_active ou subscription_active).
/// RN-003: nao envia se a quest ja foi completada hoje.
/// RN-004: respeita limite de 3 notificacoes por dia por usuario.
/// RN-005: respeita horario preferido configurado pelo usuario.
public class SendDailyQuestReminderCommandHandler(
    INotificationPreferenceRepository notificationPreferenceRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IQuestRepository questRepository,
    IPushNotificationService pushNotificationService,
    INotificationLogRepository notificationLogRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<SendDailyQuestReminderCommandHandler> logger)
    : IRequestHandler<SendDailyQuestReminderCommand, SendDailyQuestReminderResult>
{
    private const string NotificationType = "daily_quest_reminder";

    private static readonly Dictionary<string, string> PushData = new()
    {
        { "type", NotificationType },
        { "route", "/daily-quest" }
    };

    public async Task<SendDailyQuestReminderResult> Handle(
        SendDailyQuestReminderCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var today = dateTimeService.TodayUtc;
        var todayUtc = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        // US-207: itera em batches de 500 para evitar carregar toda a tabela em memoria.
        const int PageSize = 500;
        Guid? lastId = null;
        var totalEligible = 0;
        var totalSent = 0;
        var totalSkipped = 0;

        while (true)
        {
            var preferences = await notificationPreferenceRepository
                .GetPageWithPushEnabledAsync(lastId, PageSize, cancellationToken)
                ?? [];

            if (preferences.Count == 0) break;

            foreach (var preference in preferences)
            {
                totalEligible++;

                // RN-001/RN-004: push habilitado, token presente e limite diario nao atingido.
                if (!preference.CanReceiveNotificationToday(today))
                {
                    await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "daily_limit_reached", utcNow, cancellationToken);
                    logger.LogInformation("notification_send_blocked_by_limit userId={UserId} type={Type}", preference.UserId, NotificationType);
                    totalSkipped++;
                    continue;
                }

                // RN-005: respeita horario preferido — so envia apos o horario configurado.
                var localTimeOfDay = TimeOnly.FromDateTime(GetUserLocalTime(utcNow, preference.Timezone));

                if (preference.PreferredReminderTime.HasValue
                    && localTimeOfDay < preference.PreferredReminderTime.Value)
                {
                    totalSkipped++;
                    continue;
                }

                var user = await userRepository.GetByIdAsync(preference.UserId, cancellationToken);
                if (user is null)
                {
                    totalSkipped++;
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
                    totalSkipped++;
                    continue;
                }

                // RN-003: nao envia se a quest do dia ja foi completada.
                var quest = await questRepository.GetByUserIdAndDateAsync(
                    preference.UserId, "daily", todayUtc, cancellationToken);

                if (quest?.Status == "completed")
                {
                    await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "quest_completed", utcNow, cancellationToken);
                    totalSkipped++;
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
                    logger.LogInformation("daily_quest_reminder_sent userId={UserId}", preference.UserId);
                    totalSent++;
                }
                catch (Exception ex)
                {
                    await LogDecisionAsync(notificationLogRepository, preference.UserId, "failed", "push_send_failed", utcNow, cancellationToken);
                    logger.LogWarning(ex, "notification_send_failed userId={UserId} type={Type}", preference.UserId, NotificationType);
                    totalSkipped++;
                }
            }

            // US-207: salva por batch para reduzir pressao de memoria e permitir recuperacao por checkpoint.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            lastId = preferences[^1].Id;

            logger.LogInformation(
                "daily_quest_reminder_batch processed={Count} cursor={LastId}",
                preferences.Count, lastId);

            if (preferences.Count < PageSize) break;
        }

        return new SendDailyQuestReminderResult(totalEligible, totalSent, totalSkipped);
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

    private static DateTime GetUserLocalTime(DateTime utcNow, string? timezone)
    {
        var timeZoneInfo = ResolveTimeZoneInfo(timezone);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZoneInfo);
    }

    private static TimeZoneInfo ResolveTimeZoneInfo(string? timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            return TimeZoneInfo.Utc;

        var timezoneId = timezone.Trim();

        if (TryFindTimeZoneInfo(timezoneId, out var timeZoneInfo))
            return timeZoneInfo;

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timezoneId, out var windowsId)
            && TryFindTimeZoneInfo(windowsId, out timeZoneInfo))
        {
            return timeZoneInfo;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timezoneId, out var ianaId)
            && TryFindTimeZoneInfo(ianaId, out timeZoneInfo))
        {
            return timeZoneInfo;
        }

        return TimeZoneInfo.Utc;
    }

    private static bool TryFindTimeZoneInfo(string timezoneId, out TimeZoneInfo timeZoneInfo)
    {
        try
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZoneInfo = TimeZoneInfo.Utc;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            timeZoneInfo = TimeZoneInfo.Utc;
            return false;
        }
    }

    private static (string Title, string Body) GetLocalizedContent(string preferredLanguage) =>
        preferredLanguage switch
        {
            "en" => ("Your quest awaits, Hunter!", "Don't forget to complete your daily quest."),
            "es" => ("¡Tu quest te espera, Hunter!", "No olvides completar tu quest diaria."),
            "fr" => ("Ta quête t'attend, Hunter!", "N'oublie pas de compléter ta quête quotidienne."),
            _ => ("Sua quest aguarda, Hunter!", "Não esqueça de completar sua quest diária.")  // pt-BR default
        };
}
