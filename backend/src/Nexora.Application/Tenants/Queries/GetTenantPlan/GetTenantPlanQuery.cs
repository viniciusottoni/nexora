using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.GetTenantPlan;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — <c>GET /v1/platform/tenants/{id}/plan</c>.
/// Além de agregar leitura, este handler também EFETIVA de forma preguiçosa/idempotente uma
/// mudança de plano agendada cuja vigência já chegou (ver docstring do handler para a decisão de
/// design) — por isso, apesar de ser uma <see cref="IQuery{TResponse}"/> (nunca passa pelo
/// <c>TransactionBehavior</c>), o handler chama <c>SaveChangesAsync</c> explicitamente quando (e só
/// quando) essa efetivação acontece.
/// </summary>
public sealed record GetTenantPlanQuery(Guid TenantId) : IQuery<TenantPlanResponse>;
