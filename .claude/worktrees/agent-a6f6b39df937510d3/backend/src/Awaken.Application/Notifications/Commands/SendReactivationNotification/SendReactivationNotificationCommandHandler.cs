using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.SendReactivationNotification;

/// US-124: envia comunicacao de reativacao para usuarios com acesso expirado.
/// RN-001: apenas usuarios com PushEnabled=true e PushToken nao nulo.
/// RN-002: apenas usuarios com trial_expired ou subscription_expired.
/// RN-003: usuario com assinatura ativa nao recebe.
/// RN-004: frequencia minima de 7 dias entre envios (controle independente do limite diario).
/// RN-005: mensagem clara e nao enganosa, reforçando que progresso foi preservado.
public class SendReactivationNotificationCommandHandler(
    INotificationPreferenceRepository notificationPreferenceRepository,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IPushNotificationService pushNotificationService,
    INotificationLogRepository notificationLogRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<SendReactivationNotificationCommandHandler> logger)
    : IRequestHandler<SendReactivationNotificationCommand, SendReactivationNotificationResult>
{
    private const string NotificationType = "reactivation_notification";
    private const int MinDaysBetweenSends = 7;

    private static readonly Dictionary<string, string> PushData = new()
    {
        { "type", NotificationType },
        { "route", "/subscription" }
    };

    public async Task<SendReactivationNotificationResult> Handle(
        SendReactivationNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;

        var preferences = await notificationPreferenceRepository
            .GetAllWithPushEnabledAsync(cancellationToken);

        var eligible = 0;
        var sent = 0;
        var skipped = 0;

        foreach (var preference in preferences)
        {
            eligible++;

            // RN-001: push habilitado, token presente.
            // RN-004: frequencia minima de 7 dias.
            if (!preference.CanReceiveReactivationNotification(utcNow, MinDaysBetweenSends))
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "reactivation_frequency_not_met", utcNow, cancellationToken);
                logger.LogInformation("reactivation_notification_blocked userId={UserId} reason=frequency_not_met", preference.UserId);
                skipped++;
                continue;
            }

            var subscription = await subscriptionRepository.GetByUserIdAsync(preference.UserId, cancellationToken);

            // RN-003: assinante ativo nao recebe.
            if (subscription is not null
                && subscription.Plan is "monthly" or "annual"
                && subscription.ExpiresAt > utcNow)
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "subscriber_active", utcNow, cancellationToken);
                skipped++;
                continue;
            }

            // RN-002: apenas trial_expired ou subscription_expired.
            var isExpired = subscription?.Status is "trial_expired" or "subscription_expired";
            if (!isExpired)
            {
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "ignored", "access_not_expired", utcNow, cancellationToken);
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

                preference.RecordReactivationNotificationSent(utcNow);
                notificationPreferenceRepository.Update(preference);
                await LogDecisionAsync(notificationLogRepository, preference.UserId, "sent", null, utcNow, cancellationToken);

                // ADR-015: sem dados pessoais ou tokens nos logs.
                logger.LogInformation("reactivation_notification_sent userId={UserId}", preference.UserId);
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

        return new SendReactivationNotificationResult(eligible, sent, skipped);
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
            "en" => ("Your journey awaits, Hunter!", "Your progress is preserved. Come back and continue your adventure."),
            "es" => ("¡Tu aventura te espera, Hunter!", "Tu progreso está preservado. Vuelve y continúa tu jornada."),
            "fr" => ("Ton aventure t'attend, Hunter!", "Ta progression est sauvegardée. Reviens et continue ton aventure."),
            _ => ("Sua jornada te espera, Hunter!", "Seu progresso foi preservado. Volte e continue sua aventura.")  // pt-BR default
        };
}
