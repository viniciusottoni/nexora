using Nexora.Domain.Common;

namespace Nexora.Domain.Metrics;

/// <summary>
/// Alerta operacional gerado a partir de métricas, limiares (thresholds) ou eventos anômalos —
/// dirigido a papéis ou a um usuário específico.
/// </summary>
public sealed class Alert
{
    private Alert() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? StoreId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public AlertSeverity Severity { get; private set; } = AlertSeverity.Warning;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public IReadOnlyList<string> TargetRoles { get; private set; } = Array.Empty<string>();
    public Guid? TargetUserId { get; private set; }
    public string Message { get; private set; } = string.Empty;

    // TODO: tipar quando o formato do payload de alerta for definido
    public string Payload { get; private set; } = "{}";

    public DateTimeOffset RaisedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? GroupKey { get; private set; }

    public static Alert Create(
        Guid tenantId,
        string type,
        string message,
        AlertSeverity severity = AlertSeverity.Warning,
        Guid? storeId = null,
        IReadOnlyList<string>? targetRoles = null,
        string? payload = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("O tipo do alerta é obrigatório.");

        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("A mensagem do alerta é obrigatória.");

        return new Alert
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Type = type,
            Severity = severity,
            TargetRoles = targetRoles ?? Array.Empty<string>(),
            Message = message,
            Payload = payload ?? "{}",
            RaisedAt = DateTimeOffset.UtcNow
        };
    }

    public void Acknowledge(Guid acknowledgedBy)
    {
        AcknowledgedAt = DateTimeOffset.UtcNow;
        AcknowledgedBy = acknowledgedBy;
    }

    public void Resolve()
    {
        ResolvedAt = DateTimeOffset.UtcNow;
    }
}
