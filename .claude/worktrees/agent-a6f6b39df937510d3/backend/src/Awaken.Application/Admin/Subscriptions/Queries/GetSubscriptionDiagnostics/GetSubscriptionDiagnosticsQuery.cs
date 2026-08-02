using Awaken.Contracts.Admin.Subscriptions;
using MediatR;

namespace Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionDiagnostics;

/// <summary>
/// US-217: cards agregados de validações de assinatura/IAP (aprovadas/negadas/pendentes/falhas).
/// RN-002: PendingThresholdMinutes define a partir de quantos minutos uma transação pendente
/// passa a contar como "concessão pendente" nos indicadores.
/// </summary>
public record GetSubscriptionDiagnosticsQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    int PendingThresholdMinutes = 30) : IRequest<SubscriptionDiagnosticsResponse>;
