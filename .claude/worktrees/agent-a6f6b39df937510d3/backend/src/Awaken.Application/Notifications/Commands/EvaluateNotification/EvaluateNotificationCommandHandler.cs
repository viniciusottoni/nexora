using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Notifications;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Notifications.Commands.EvaluateNotification;

/// US-095: avalia elegibilidade de notificação e registra decisão no NotificationLog.
public class EvaluateNotificationCommandHandler(
    INotificationEligibilityService eligibilityService,
    INotificationLogRepository notificationLogRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<EvaluateNotificationCommandHandler> logger)
    : IRequestHandler<EvaluateNotificationCommand, EvaluateNotificationResult>
{
    public async Task<EvaluateNotificationResult> Handle(
        EvaluateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;

        var result = await eligibilityService.EvaluateAsync(
            request.UserId,
            request.NotificationType,
            utcNow,
            cancellationToken);

        var decisionStatus = result.Allowed ? "sent" : "ignored";

        var log = NotificationLog.Create(
            request.UserId,
            request.NotificationType,
            decisionStatus,
            result.BlockReason,
            utcNow);

        await notificationLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // US-095: analytics events (ADR-015 — sem dados pessoais).
        if (!result.Allowed && result.BlockReason == "daily_limit_reached")
            logger.LogInformation(
                "notification_send_blocked_by_limit userId={UserId} type={Type}",
                request.UserId,
                request.NotificationType);

        logger.LogInformation(
            "notification_send_decision_logged logId={LogId} userId={UserId} type={Type} status={Status} reason={Reason}",
            log.Id,
            request.UserId,
            request.NotificationType,
            decisionStatus,
            result.BlockReason ?? "none");

        return new EvaluateNotificationResult(result.Allowed, result.BlockReason, log.Id);
    }
}
