using Awaken.Contracts.Admin.Economy;
using MediatR;

namespace Awaken.Application.Admin.Economy.Queries.GetGoldEconomySummary;

/// <summary>
/// US-229: resumo agregado da economia Gold (janela padrão: últimos 30 dias).
/// RN-003: nenhum dado de pagamento/provider.
/// </summary>
public record GetGoldEconomySummaryQuery(DateTime? FromUtc, DateTime? ToUtc)
    : IRequest<GoldEconomySummaryResponse>;
