using Awaken.Contracts.Admin.Subscriptions;
using MediatR;

namespace Awaken.Application.Admin.Subscriptions.Queries.GetSubscriptionEvents;

/// <summary>
/// US-217: listagem paginada de eventos de assinatura/IAP, filtrável por
/// tipo, loja, status, plano, produto, ambiente e usuário.
/// </summary>
public record GetSubscriptionEventsQuery(
    string? Type,
    string? Store,
    string? Status,
    string? Plan,
    string? Product,
    string? Environment,
    Guid? UserId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize,
    int PendingThresholdMinutes = 30) : IRequest<SubscriptionEventListResponse>;
