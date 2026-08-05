using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Tenants.Queries.GetSupportAccessHistory;

/// <summary>
/// US-145 §10 "Histórico de acessos sempre disponível ao cliente, sem precisar pedir" — porta de
/// <c>GET /v1/tenant/support-access-history</c>. Sem filtro: é a trilha completa do próprio
/// tenant (RLS já restringe a leitura a ele), ordenada da concessão mais recente para a mais antiga.
/// </summary>
public sealed record GetSupportAccessHistoryQuery : IQuery<SupportAccessListResponse>;
