using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tables;

namespace Nexora.Application.Tables.Queries.GetTableMap;

/// <summary>Critério de ordenação do mapa de mesas (US-023 §10: "urgência como padrão, com opção por número de mesa").</summary>
public enum TableMapSortBy
{
    Urgency,
    Label
}

/// <summary>
/// Porta de <c>GET /v1/tables</c> (US-023 §7). <paramref name="MineOnly"/> filtra para as mesas cuja
/// sessão aberta pertence ao garçom autenticado (<see cref="Nexora.Application.Abstractions.Security.ICurrentTenantContext.UserId"/>) —
/// mesa livre nunca aparece com o filtro ligado, porque não tem garçom nenhum associado (Gherkin
/// "Filtro por responsabilidade").
/// </summary>
public sealed record GetTableMapQuery(bool MineOnly, TableMapSortBy SortBy) : IQuery<TableMapResponse>;
