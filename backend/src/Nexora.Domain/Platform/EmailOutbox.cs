using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Fila de e-mails transacionais com retentativa. Registro quase sempre append-write,
/// sem regra de negócio própria além do ciclo de envio.
/// </summary>
public sealed class EmailOutbox
{
    private EmailOutbox() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string Template { get; private set; } = string.Empty;
    public string PayloadEncrypted { get; private set; } = string.Empty;
    public string Status { get; private set; } = "PENDING";
    public short Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static EmailOutbox Create(Guid tenantId, string recipient, string template, string payloadEncrypted)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new DomainException("O destinatário do e-mail é obrigatório.");

        if (string.IsNullOrWhiteSpace(template))
            throw new DomainException("O template do e-mail é obrigatório.");

        return new EmailOutbox
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Recipient = recipient,
            Template = template,
            PayloadEncrypted = payloadEncrypted,
            Status = "PENDING",
            Attempts = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkSent()
    {
        Status = "SENT";
        SentAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error, DateTimeOffset? nextAttemptAt)
    {
        Attempts++;
        LastError = error;
        NextAttemptAt = nextAttemptAt;
        Status = nextAttemptAt is null ? "FAILED" : "PENDING";
    }
}
