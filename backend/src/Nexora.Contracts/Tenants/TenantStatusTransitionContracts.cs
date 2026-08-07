namespace Nexora.Contracts.Tenants;

/// <summary>
/// US-153 · Ciclo de vida do estabelecimento — porta de
/// <c>POST /v1/platform/tenants/{id}/status-transitions</c>. <see cref="TargetStatus"/> aceita
/// apenas os alvos que um administrador de plataforma pode decidir diretamente
/// (<c>ACTIVE</c>/<c>SUSPENDED</c>/<c>CANCELLED</c> — nunca <c>PROVISIONED</c>/<c>INSTALLING</c>,
/// que só o sistema atinge). Espelha <c>packages/contracts/src/tenants.ts</c>
/// (<c>tenantStatusTransitionRequestSchema</c>); as duas listas precisam continuar em sincronia.
/// </summary>
/// <param name="TargetStatus">Rótulo de fio de <see cref="Nexora.Domain.Platform.TenantStatus"/> (<c>Enum.TryParse</c> ignorando caixa).</param>
/// <param name="Reason">Motivo obrigatório e substantivo (doc §10) — validado no handler para devolver 422 REASON_REQUIRED, não 400 genérico.</param>
/// <param name="EffectiveAt">Instante de efetivação — nulo usa o instante do processamento.</param>
public sealed record TenantStatusTransitionRequest(string TargetStatus, string Reason, DateTimeOffset? EffectiveAt);

public sealed record TenantStatusTransitionResponse(
    Guid TenantId,
    string PreviousStatus,
    string Status,
    int Version,
    DateTimeOffset ChangedAt);
