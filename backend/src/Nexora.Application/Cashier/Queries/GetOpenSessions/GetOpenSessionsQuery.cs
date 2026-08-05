using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Queries.GetOpenSessions;

/// <summary>Critério de ordenação do painel do caixa (US-050 §10: "urgência como padrão, com opção por número de mesa" — mesmo vocabulário de <c>TableMapSortBy</c>, US-023).</summary>
public enum GetOpenSessionsSortBy
{
    Urgency,
    Table
}

/// <summary>
/// Porta de <c>GET /v1/cash/open-sessions</c> (US-050 §7). <paramref name="Search"/> filtra por
/// mesa (<c>dining_table.label</c>) OU por comanda (<see cref="OpenSessionEntryResponse.OrderCode"/>)
/// — substring, sem diferenciar maiúsculas/minúsculas (US-050 §10: "busca com foco automático, para
/// operação por teclado" pede tolerância a digitação parcial). Nulo/vazio não filtra nada.
/// </summary>
public sealed record GetOpenSessionsQuery(string? Search, GetOpenSessionsSortBy SortBy) : IQuery<OpenSessionsResponse>;
