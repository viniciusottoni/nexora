using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// US-153 · Ciclo de vida do estabelecimento — histórico imutável de transições de status (doc §8,
/// tabela <c>tenant_status_history</c>). Append-only, sem método de mutação além de <see cref="Create"/>
/// (mesmo padrão de <see cref="DomainEvent"/>/<see cref="AuditLog"/>: registro de fato, sem regra de
/// negócio própria — a validação da transição em si é responsabilidade de
/// <see cref="TenantStatusTransitions"/>/<see cref="Tenant.TransitionStatus"/>, chamada ANTES de
/// construir esta linha).
/// </summary>
public sealed class TenantStatusHistory
{
    private TenantStatusHistory() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public TenantStatus PreviousStatus { get; private set; }
    public TenantStatus NewStatus { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }

    /// <summary>"PLATFORM_ADMIN" (ação administrativa) ou "SYSTEM" (ativação automática/assistida do roteiro de implantação, registro de instalação edge).</summary>
    public string Origin { get; private set; } = string.Empty;

    /// <summary>Correlação com o <see cref="DomainEvent"/> <c>tenant.status_changed</c> emitido pela mesma transição (doc §8 "correlação") — mesmo padrão de <see cref="AuditLog.DomainEventId"/>.</summary>
    public Guid? DomainEventId { get; private set; }

    public DateTimeOffset EffectiveAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public static TenantStatusHistory Create(
        Guid tenantId,
        TenantStatus previousStatus,
        TenantStatus newStatus,
        string reason,
        string origin,
        DateTimeOffset effectiveAt,
        Guid? actorId = null,
        Guid? domainEventId = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da transição de status é obrigatório.");

        if (string.IsNullOrWhiteSpace(origin))
            throw new DomainException("A origem da transição de status é obrigatória.");

        return new TenantStatusHistory
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Reason = reason.Trim(),
            ActorId = actorId,
            Origin = origin,
            DomainEventId = domainEventId,
            EffectiveAt = effectiveAt,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
