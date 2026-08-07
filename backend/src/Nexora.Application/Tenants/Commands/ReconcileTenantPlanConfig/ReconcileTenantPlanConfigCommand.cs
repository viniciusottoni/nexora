using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Commands.ReconcileTenantPlanConfig;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial —
/// <c>POST /v1/platform/tenants/{id}/plan/reconciliations</c>. Endpoint ADICIONAL ao contrato
/// abreviado do §7 da US (que só lista <c>GET /plans</c>, <c>GET /tenants/{id}/plan</c> e
/// <c>PUT /tenants/{id}/plan</c>) — necessário para que a UI possa de fato "oferecer reconciliação
/// idempotente e auditada" (§4, cenário "Divergência detectada") como uma ação EXPLÍCITA do
/// administrador, já que <c>GetTenantPlanQueryHandler</c> deliberadamente só DETECTA e reporta a
/// divergência, nunca a corrige sozinho (§10 "sem correção automática silenciosa"). Decisão de
/// implementação registrada no relatório final da tarefa.
/// </summary>
public sealed record ReconcileTenantPlanConfigCommand(Guid TenantId, Guid? ActorId) : ICommand<TenantPlanReconcileResponse>;
