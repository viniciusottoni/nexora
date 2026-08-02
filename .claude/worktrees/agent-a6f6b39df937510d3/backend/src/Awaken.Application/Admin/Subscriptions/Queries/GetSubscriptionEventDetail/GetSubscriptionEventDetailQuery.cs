using Awaken.Contracts.Admin.Subscriptions;
using MediatR;

namespace Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionEventDetail;

/// <summary>
/// US-217: detalhe seguro de uma validação de assinatura/IAP.
/// <paramref name="Source"/> distingue a origem do registro ("revenuecat_event" | "iap_ledger"),
/// já que Id não é globalmente único entre as duas tabelas de origem.
/// </summary>
public record GetSubscriptionEventDetailQuery(
    Guid Id,
    string Source,
    int PendingThresholdMinutes = 30) : IRequest<SubscriptionEventDetailResponse>;
