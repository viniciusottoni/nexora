using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Queries.ListPlatformPlans;

/// <summary>US-154 · Gestão de planos e configuração comercial — <c>GET /v1/platform/plans</c>, catálogo completo (ativos e inativos, para a tela de administração distinguir os dois) ordenado por código.</summary>
public sealed record ListPlatformPlansQuery : IQuery<PlatformPlanListResponse>;
