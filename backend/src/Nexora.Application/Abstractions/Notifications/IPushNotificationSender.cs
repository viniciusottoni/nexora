namespace Nexora.Application.Abstractions.Notifications;

/// <summary>US-081 §2 "o push é enviado pela nuvem" — Web Push/VAPID (RFC 8291/8292), implementado em Infrastructure (<c>WebPushSender</c>), nunca chamado pelo edge.</summary>
public interface IPushNotificationSender
{
    Task SendAsync(PushTarget target, PushPayload payload, CancellationToken cancellationToken);
}

/// <summary>Assinatura de destino (US-081 §7, <c>push_subscription</c>).</summary>
public sealed record PushTarget(string Endpoint, string P256dhKey, string AuthKey);

/// <summary>Conteúdo exibido pela notificação do navegador (US-081 §10, "severidade diferenciada por cor/som").</summary>
public sealed record PushPayload(string Title, string Body, string Severity, Guid AlertId);
