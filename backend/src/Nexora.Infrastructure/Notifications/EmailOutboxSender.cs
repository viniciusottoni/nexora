using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// Implementação de <see cref="IEmailSender"/> que só enfileira — nunca envia de forma síncrona.
/// Grava em <see cref="EmailOutbox"/> usando o mesmo <see cref="IApplicationDbContext"/> do
/// handler que a chamou, então o registro entra na mesma transação do comando (ADR-006) e é
/// persistido pelo <c>TransactionBehavior</c>, não aqui. Um <c>BackgroundService</c> de entrega
/// (porta de <c>InvitationDeliveryWorker</c>) fica fora do escopo deste módulo — consome a fila
/// depois, decifrando com <see cref="EmailPayloadCipher.Decrypt"/>.
/// </summary>
public sealed class EmailOutboxSender : IEmailSender
{
    private readonly IApplicationDbContext _db;
    private readonly EmailOutboxOptions _options;

    public EmailOutboxSender(IApplicationDbContext db, IOptions<EmailOutboxOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public Task EnqueueAsync(
        Guid tenantId,
        string recipient,
        string template,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        var payloadEncrypted = EmailPayloadCipher.Encrypt(variables, _options.EncryptionKey);
        var outbox = EmailOutbox.Create(tenantId, recipient, template, payloadEncrypted);
        _db.EmailOutboxes.Add(outbox);
        return Task.CompletedTask;
    }
}
