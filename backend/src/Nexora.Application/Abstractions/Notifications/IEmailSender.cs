namespace Nexora.Application.Abstractions.Notifications;

/// <summary>
/// Envio de e-mail transacional (convite de dono, redefinição de senha etc.). A implementação
/// em Infrastructure NÃO envia de forma síncrona — grava em <c>Nexora.Domain.Platform.EmailOutbox</c>
/// dentro da mesma transação do handler (ADR-006: nada muda de estado sem seu evento/efeito
/// persistido junto) e um <c>BackgroundService</c> entrega de fato, com retentativa. Porta de
/// <c>invitation-delivery.ts</c> (InvitationDeliveryWorker) do NestJS original.
/// </summary>
public interface IEmailSender
{
    /// <summary>Enfileira um e-mail para entrega — não garante envio imediato, só a durabilidade do enfileiramento.</summary>
    Task EnqueueAsync(
        Guid tenantId,
        string recipient,
        string template,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default);
}
