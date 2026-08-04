using Microsoft.Extensions.Logging;
using Nexora.Application.Abstractions.Notifications;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// <see cref="IPushNotificationSender"/> de fallback para ambiente sem chaves VAPID configuradas
/// (dev/CI/teste) — mesmo espírito de <see cref="LoggingEmailDispatcher"/>: o mecanismo de entrega
/// (<c>DeliverPendingPushCommand</c>, <c>Alert.MarkPushed</c>) roda de ponta a ponta, só o envio real
/// ao provedor de push é substituído por log estruturado.
/// </summary>
public sealed class LoggingPushNotificationSender : IPushNotificationSender
{
    private readonly ILogger<LoggingPushNotificationSender> _logger;

    public LoggingPushNotificationSender(ILogger<LoggingPushNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(PushTarget target, PushPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Push não enviado por VAPID real (WebPush:PublicKeyBase64Url não configurado) — Endpoint={Endpoint} " +
            "Severidade={Severity} Titulo={Title}\n{Body}",
            target.Endpoint, payload.Severity, payload.Title, payload.Body);

        return Task.CompletedTask;
    }
}
