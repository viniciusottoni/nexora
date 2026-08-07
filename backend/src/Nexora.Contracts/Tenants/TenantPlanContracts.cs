namespace Nexora.Contracts.Tenants;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — contratos de
/// <c>GET /v1/platform/plans</c>, <c>GET /v1/platform/tenants/{id}/plan</c> e
/// <c>PUT /v1/platform/tenants/{id}/plan</c>. Espelha
/// <c>packages/contracts/src/tenant-plan.ts</c> (zod) — as duas listas precisam continuar em
/// sincronia, mesma convenção de <see cref="TenantStatusTransitionContracts"/>.
/// </summary>
public sealed record PlatformPlanSummaryResponse(
    string Code,
    string Name,
    bool Active,
    IReadOnlyList<string> Capabilities);

public sealed record PlatformPlanListResponse(IReadOnlyList<PlatformPlanSummaryResponse> Data);

/// <summary>Agendamento pendente de um tenant — nulo quando não há mudança de plano aguardando vigência.</summary>
public sealed record TenantPlanScheduledResponse(string Plan, DateTimeOffset EffectiveAt);

/// <summary>Linha temporal imutável de uma solicitação de mudança de plano, pendente ou já efetivada.</summary>
public sealed record TenantPlanHistoryEntryResponse(
    string Previous,
    string Next,
    DateTimeOffset RequestedAt,
    DateTimeOffset EffectiveAt,
    DateTimeOffset? AppliedAt,
    string Reason,
    Guid? ActorId);

/// <summary>
/// Resposta de <c>GET /v1/platform/tenants/{id}/plan</c>. <see cref="Version"/> não aparece no
/// exemplo abreviado da US (§7), mas é necessário na prática para o próximo <c>If-Match</c> do
/// <c>PUT</c> — mesmo papel de <c>TenantOverviewResponse.tenant.statusVersion</c> para
/// <c>status-transitions</c> (US-153); decisão de implementação registrada no relatório desta
/// tarefa. <see cref="Consistent"/> é <c>false</c> quando as capacidades efetivas
/// (<c>tenant_config.plan_capabilities</c>) não correspondem às do plano corrente no catálogo —
/// nunca corrigido automaticamente (§10 "sem correção automática silenciosa").
/// </summary>
public sealed record TenantPlanResponse(
    string Current,
    IReadOnlyList<string> EffectiveCapabilities,
    TenantPlanScheduledResponse? Scheduled,
    bool Consistent,
    int Version,
    IReadOnlyList<TenantPlanHistoryEntryResponse> History);

/// <summary>Corpo de <c>PUT /v1/platform/tenants/{id}/plan</c> — motivo e vigência obrigatórios (§10).</summary>
public sealed record TenantPlanUpdateRequest(string Plan, DateTimeOffset? EffectiveAt, string Reason);

public sealed record TenantPlanUpdateResponse(string Current, TenantPlanScheduledResponse? Scheduled, int Version);

/// <summary>
/// Resposta de <c>POST /v1/platform/tenants/{id}/plan/reconciliations</c> — endpoint ADICIONAL,
/// não listado no exemplo abreviado do §7 da US, necessário para que a UI possa de fato "oferecer
/// reconciliação idempotente e auditada" (§4, cenário "Divergência detectada") sem corrigir nada
/// silenciosamente durante o simples <c>GET</c>. Decisão de implementação registrada no relatório
/// final desta tarefa.
/// </summary>
public sealed record TenantPlanReconcileResponse(
    string Current,
    IReadOnlyList<string> EffectiveCapabilities,
    bool Consistent,
    bool Changed);
