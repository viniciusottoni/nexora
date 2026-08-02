using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.SendTrialEndingNotification;

/// US-123: avisa usuarios com trial proximo do fim (<=3 dias) via push notification.
/// RN-001: apenas usuarios com PushEnabled=true e PushToken nao nulo.
/// RN-002: apenas usuarios com trial ativo (trial_active).
/// RN-003: usuario com assinatura ativa nao recebe aviso.
/// RN-004: respeita limite diario de notificacoes.
/// RN-005: conteudo claro e nao enganoso, com deep link para paywall.
public class SendTrialEndingNotificationCommandHandler(
    INotificationPreferenceRepository notificationPreferenceRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IPushNotificationService pushNotificationService,
    INotificationLogRepository notificationLogRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<SendTrialEndingNotificationCommandHandler> logger)
    : IRequestHandler<SendTrialEndingNotificationCommand, SendTrialEndingNotificationResult>
{
    private const string NotificationType = "trial_ending_notification";
    private const int DaysThreshold = 3;

    private static readonly Dictionary<string, string> PushData = new()
    {
        { "type", NotificationType },
        { "route", "/subscription" }
    };

    public async Task<SendTrialEndingNotificationResult> Handle(
        SendTrialEndingNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var today = dateTimeService.TodayUtc;

        var preferences = await notificationPreferenceRepository
            .GetAllWithPushEnabledAsync(cancellationToken);

        var eligible = 0;
        var sent = 0;
        var skipped = 0;

        foreach (var preference in preferences)
        {
            eligible++;

            // RN-004: limite diario nao atingido.
            if (!preference.CanReceiveNotificationToday(today))
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "daily_limit_reached", utcNow, cancellationToken);
                logger.LogInformation("notification_send_blocked_by_limit userId={UserId} type={Type}", preference.UserId, NotificationType);
                skipped++;
                continue;
            }

            var subscription = await subscriptionRepository.GetByUserIdAsync(preference.UserId, cancellationToken);

            // RN-003: assinante ativo nao recebe.
            if (subscription?.Plan is "monthly" or "annual" && subscription.ExpiresAt > utcNow)
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "subscriber_active", utcNow, cancellationToken);
                skipped++;
                continue;
            }

            // RN-002: apenas trial ativo.
            if (subscription is null || subscription.Status != "trial_active" || !subscription.TrialEndsAt.HasValue)
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "not_trial_active", utcNow, cancellationToken);
                skipped++;
                continue;
            }

            // Verifica se trial esta proximo do fim (<=3 dias).
            var daysRemaining = (subscription.TrialEndsAt.Value - utcNow).TotalDays;
            if (daysRemaining > DaysThreshold || daysRemaining < 0)
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "trial_not_ending_soon", utcNow, cancellationToken);
                skipped++;
                continue;
            }

            var user = await userRepository.GetByIdAsync(preference.UserId, cancellationToken);
            if (user is null)
            {
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
                logger.LogInformation("trial_ending_notification_sent userId={UserId} daysRemaining={Days}", preference.UserId, (int)daysRemaining);
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

        return new SendTrialEndingNotificationResult(eligible, sent, skipped);
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
            "en" => ("Your trial is ending soon!", "Your free trial ends in 3 days. Subscribe now to keep your progress."),
            "es" => ("¡Tu prueba está por terminar!", "Tu prueba gratuita termina en 3 días. Suscríbete para conservar tu progreso."),
            "fr" => ("Ton essai se termine bientôt!", "Ton essai gratuit se termine dans 3 jours. Abonne-toi pour garder ta progression."),
            _ => ("Seu trial está acabando!", "Seu período gratuito termina em 3 dias. Assine agora para manter seu progresso.")  // pt-BR default
        };
}
