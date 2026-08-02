namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// Entrega efetiva de um e-mail já decifrado de <c>email_outbox</c> — a "última milha" que
/// <see cref="EmailOutboxDeliveryWorker"/> chama depois de decifrar o envelope com
/// <see cref="EmailPayloadCipher.Decrypt"/>. Duas implementações concretas: <see cref="SmtpEmailDispatcher"/>
/// (SMTP real via <c>System.Net.Mail</c>) e <see cref="LoggingEmailDispatcher"/> (ambiente sem SMTP
/// configurado — ex.: dev/CI). Vive em Infrastructure, não em Application (ADR-039): só
/// <see cref="EmailOutboxDeliveryWorker"/> a consome, e ela nunca cruza a porta <c>IEmailSender</c>
/// que Application conhece (essa é só para enfileirar, nunca para enviar de fato).
/// </summary>
public interface IEmailDispatcher
{
    Task SendAsync(
        string recipient,
        string template,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken);
}
