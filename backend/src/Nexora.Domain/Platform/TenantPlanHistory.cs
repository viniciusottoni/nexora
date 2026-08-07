using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — linha do tempo de mudanças de plano
/// comercial de um tenant (<c>tenant_plan_history</c>), com <c>tenant_id</c> e RLS normal (ADR-004)
/// — diferente de <see cref="PlatformPlan"/> (catálogo global), esta é uma tabela de NEGÓCIO comum,
/// mesma observação já registrada em <see cref="TenantStatusHistory"/> para o histórico de status.
/// </summary>
/// <remarks>
/// Diferente de <see cref="TenantStatusHistory"/> (inserida uma única vez, já com o estado final),
/// esta entidade tem DUAS fases porque uma mudança de plano pode ser AGENDADA para o futuro
/// (US-154 §4, cenário "Mudança de plano com vigência"): <see cref="Create"/> grava a intenção
/// assim que o administrador confirma (aparece no histórico imediatamente, <see cref="AppliedAt"/>
/// nulo se a vigência é futura), e <see cref="MarkApplied"/> completa a linha quando o plano
/// efetivamente entra em vigor (na hora, se <c>effectiveAt &lt;= now</c>, ou mais tarde, quando
/// <c>GetTenantPlanQueryHandler</c> detecta que a data já passou — ver docstring dessa classe para
/// a decisão de efetivação preguiçosa/idempotente). <see cref="DomainEventId"/> só é preenchido na
/// efetivação (é quando <c>tenant.plan_changed</c>/EVT-057 é emitido, nunca no agendamento).
/// </remarks>
public sealed class TenantPlanHistory
{
    private TenantPlanHistory() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string PreviousPlan { get; private set; } = string.Empty;
    public string NextPlan { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }

    /// <summary>Instante em que o administrador confirmou a mudança (sempre preenchido, mesmo para vigência futura).</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Data/hora em que o plano deve (ou deveria) entrar em vigor.</summary>
    public DateTimeOffset EffectiveAt { get; private set; }

    /// <summary>Nulo enquanto a mudança está apenas agendada; preenchido no instante real da efetivação (<see cref="MarkApplied"/>).</summary>
    public DateTimeOffset? AppliedAt { get; private set; }

    /// <summary>Correlação com o <see cref="DomainEvent"/> <c>tenant.plan_changed</c> (EVT-057) — preenchido só na efetivação, mesmo padrão de <see cref="TenantStatusHistory.DomainEventId"/>/<see cref="AuditLog.DomainEventId"/>.</summary>
    public Guid? DomainEventId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    /// <summary>Verdadeiro quando a mudança ainda não entrou em vigor (nenhum <see cref="AppliedAt"/>) — usado por <c>GetTenantPlanQueryHandler</c> para achar o "próximo" agendamento de um tenant.</summary>
    public bool IsPending => AppliedAt is null;

    public static TenantPlanHistory Create(
        Guid tenantId,
        string previousPlan,
        string nextPlan,
        string reason,
        DateTimeOffset requestedAt,
        DateTimeOffset effectiveAt,
        Guid? actorId = null)
    {
        if (string.IsNullOrWhiteSpace(previousPlan))
            throw new DomainException("O plano anterior é obrigatório no histórico.");

        if (string.IsNullOrWhiteSpace(nextPlan))
            throw new DomainException("O novo plano é obrigatório no histórico.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo da mudança de plano é obrigatório.");

        return new TenantPlanHistory
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            PreviousPlan = previousPlan,
            NextPlan = nextPlan,
            Reason = reason.Trim(),
            ActorId = actorId,
            RequestedAt = requestedAt,
            EffectiveAt = effectiveAt,
            AppliedAt = null,
            CreatedAt = requestedAt,
        };
    }

    /// <summary>
    /// Completa a linha quando o plano efetivamente entra em vigor — idempotente por construção: o
    /// chamador (<c>UpdateTenantPlanCommandHandler</c> para vigência imediata, ou
    /// <c>GetTenantPlanQueryHandler</c> para efetivação preguiçosa de um agendamento cuja data já
    /// passou) só chama isto UMA vez por linha, sempre filtrando por <see cref="IsPending"/> antes.
    /// </summary>
    public void MarkApplied(DateTimeOffset appliedAt, Guid domainEventId)
    {
        if (!IsPending)
            throw new DomainException("Esta mudança de plano já foi efetivada.");

        AppliedAt = appliedAt;
        DomainEventId = domainEventId;
    }
}
