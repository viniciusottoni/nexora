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

    /// <summary>
    /// Início da janela de agrupamento que este alerta ocupa (US-083) — todo alerta do MESMO tipo
    /// raised dentro de <c>[GroupWindowStart, GroupWindowStart + groupWindowSeconds)</c> recebe o
    /// mesmo <see cref="GroupKey"/>; um alerta fora da janela abre um grupo (e uma notificação) novos.
    /// Nulo para alertas cujo tipo não tem agrupamento configurado (ex.: CASH_DIVERGENCE, sempre
    /// pontual, um por sessão de caixa).
    /// </summary>
    public DateTimeOffset? GroupWindowStart { get; private set; }

    /// <summary>Marcado pelo worker de entrega de push da nuvem (US-081) — evita reenviar o mesmo alerta.</summary>
    public DateTimeOffset? PushedAt { get; private set; }

    public static Alert Create(
        Guid tenantId,
        string type,
        string message,
        AlertSeverity severity = AlertSeverity.Warning,
        Guid? storeId = null,
        IReadOnlyList<string>? targetRoles = null,
        string? payload = null,
        string? entityType = null,
        Guid? entityId = null,
        Guid? targetUserId = null,
        string? groupKey = null,
        DateTimeOffset? groupWindowStart = null)
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
            EntityType = entityType,
            EntityId = entityId,
            TargetRoles = targetRoles ?? Array.Empty<string>(),
            TargetUserId = targetUserId,
            Message = message,
            Payload = payload ?? "{}",
            RaisedAt = DateTimeOffset.UtcNow,
            GroupKey = groupKey,
            GroupWindowStart = groupWindowStart
        };
    }

    /// <summary>
    /// Altera o conjunto de papéis-alvo do alerta (US-025 §4, cenário "Escalonamento por falta de
    /// atendimento": "o alerta deve escalar para os demais garçons do ambiente") — chamado pelo
    /// worker de escalonamento (<c>WaiterCallEscalationWorker</c>) sobre um alerta já existente, em
    /// vez de criar um segundo alerta para a MESMA chamada (que quebraria a garantia de "chamada
    /// repetida não duplica").
    /// </summary>
    public void Escalate(IReadOnlyList<string> targetRoles)
    {
        TargetRoles = targetRoles;
    }

    /// <summary>
    /// US-080 §4, cenário "Escalonamento por duração": severidade sobe quando o alerta permanece
    /// ativo além do limiar de escalonamento do tipo — nunca desce sozinha (só <see cref="Resolve"/>
    /// encerra o alerta).
    /// </summary>
    public void IncreaseSeverity(AlertSeverity severity)
    {
        if (severity > Severity)
        {
            Severity = severity;
        }
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

    public void MarkPushed()
    {
        PushedAt = DateTimeOffset.UtcNow;
    }
}
