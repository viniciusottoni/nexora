using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Shared;
using Nexora.Application.Orders.Commands.CancelOrderItem;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Auth;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Commands.CancelOrder;

/// <summary>
/// US-033 §4/§7, cenário "Cancelamento de pedido inteiro" — cancela todos os itens ATIVOS (não
/// servidos, não já cancelados) do pedido e o pedido em si, na MESMA operação/transação
/// (<c>TransactionBehavior</c>, ADR-037). Autorização elevada é exigida uma ÚNICA vez
/// (<c>CANCEL_STARTED_ITEM</c>, mesma ação de <see cref="CancelOrderItemCommandHandler"/>, já que o
/// catálogo (ADR-023) não reserva uma ação distinta para "cancelar pedido") quando QUALQUER item
/// ativo já foi iniciado — mesmo raciocínio e mesma verificação de contexto (por PEDIDO, não por
/// item) de <see cref="CancelOrderItemCommandHandler"/>; ver a docstring dele para o detalhe do
/// mecanismo de elevação pontual e do gap de RN-008/US-105 (registro de perda, Fase 2).
/// </summary>
internal sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<CancelOrderResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IAuthorizationTokenValidator _authorizationTokenValidator;

    public CancelOrderCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        ICurrentTenantContext tenantContext,
        IAuthorizationTokenValidator authorizationTokenValidator)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _tenantContext = tenantContext;
        _authorizationTokenValidator = authorizationTokenValidator;
    }

    public async Task<Result<CancelOrderResponse>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Session)
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result<CancelOrderResponse>.Failure("Pedido não encontrado.", ApiErrorCodes.OrderNotFound);
        }

        // US-033 §4, cenário "Pedido fechado não cancela": a orientação aponta o fluxo de estorno
        // (RF-CXA-13/Fase 2) — 409, nunca 422 (não é erro de validação de entrada, é conflito de
        // estado, família CONFLICT_* do ADR-021).
        if (order.Status is OrderStatus.Closed or OrderStatus.Cancelled)
        {
            return Result<CancelOrderResponse>.Failure(
                "Pedido fechado ou já cancelado não pode ser cancelado — utilize o fluxo de estorno.",
                ApiErrorCodes.InvalidStateTransition);
        }

        var activeItems = order.Items.Where(i => i.Status != OrderItemStatus.Cancelled).ToList();

        if (activeItems.Any(i => i.Status == OrderItemStatus.Served))
        {
            return Result<CancelOrderResponse>.Failure(
                "Pedido tem item já servido — não é possível cancelar o pedido inteiro. Cancele os itens pendentes individualmente.",
                ApiErrorCodes.InvalidStateTransition);
        }

        // wasStarted (nível pedido) — RN-008: qualquer item ativo já iniciado exige autorização
        // pelo MESMO raciocínio de OrderCancellationPolicy, aplicado ao conjunto de itens.
        var wasStarted = activeItems.Any(i => OrderCancellationPolicy.RequiresAuthorization(i.Status));
        var actorId = _tenantContext.UserId ?? Guid.Empty;
        var deviceId = _tenantContext.DeviceId;
        var now = DateTimeOffset.UtcNow;

        Guid? authorizedBy = null;

        if (wasStarted)
        {
            var contextHash = AuthorizationContextHasher.Hash(
                new Dictionary<string, object?> { ["orderId"] = order.Id.ToString() });
            var grantResult = await _authorizationTokenValidator.ValidateAsync(
                request.AuthorizationToken, CancelOrderItemCommandHandler.CancelStartedItemAction, cancellationToken);

            if (grantResult.IsFailure || !string.Equals(grantResult.Value!.ContextHash, contextHash, StringComparison.Ordinal))
            {
                // IPersistsStateOnFailureCommand (ver docstring da interface): a TransactionBehavior
                // só COMITA em caso de falha, sem chamar SaveChangesAsync — o handler grava explicitamente.
                _db.AuditLogs.Add(AuditLog.Create(
                    order.TenantId,
                    action: "ORDER_CANCEL_DENIED",
                    entity: "order",
                    occurredAt: now,
                    storeId: order.StoreId,
                    actorId: actorId == Guid.Empty ? null : actorId,
                    deviceId: deviceId,
                    entityId: order.Id,
                    after: JsonSerializer.Serialize(new { requiredAction = CancelOrderItemCommandHandler.CancelStartedItemAction })));
                await _db.SaveChangesAsync(cancellationToken);

                return Result<CancelOrderResponse>.Failure(
                    "Pedido tem item já iniciado. É necessária autorização de perfil superior.",
                    ApiErrorCodes.AuthorizationRequired,
                    new Dictionary<string, string[]> { ["action"] = new[] { CancelOrderItemCommandHandler.CancelStartedItemAction } });
            }

            authorizedBy = grantResult.Value.AuthorizedBy;
        }

        // Snapshot do estado ANTES de mutar — wasStarted É POR ITEM no evento/auditoria de cada
        // item (RN-008 se aplica item a item, mesmo que a autorização tenha sido pedida uma vez
        // só para o pedido).
        var itemSnapshots = activeItems
            .Select(i => (Item: i, WasStarted: OrderCancellationPolicy.RequiresAuthorization(i.Status), BeforeStatus: OrderItemStatusLabels.ToWireStatus(i.Status)))
            .ToList();

        foreach (var (item, _, _) in itemSnapshots)
        {
            item.Cancel(request.Reason, actorId, authorizedBy);
        }

        order.Cancel(request.Reason, actorId, authorizedBy);
        var cancelledAt = order.CancelledAt ?? now;

        _db.AuditLogs.Add(AuditLog.Create(
            order.TenantId,
            action: "ORDER_CANCELLED",
            entity: "order",
            occurredAt: cancelledAt,
            storeId: order.StoreId,
            actorId: actorId == Guid.Empty ? null : actorId,
            authorizedBy: authorizedBy,
            deviceId: deviceId,
            entityId: order.Id,
            before: JsonSerializer.Serialize(new { status = "PLACED_OR_IN_PRODUCTION", total = order.Total }),
            after: JsonSerializer.Serialize(new { status = "CANCELLED", total = order.Total, itemsCancelled = itemSnapshots.Count }),
            reason: request.Reason));

        // EVT-016 order.cancelled — payload exigido (US-033 §6): reason, authorizedBy, stage.
        _db.DomainEvents.Add(DomainEvent.Create(
            order.TenantId,
            type: "order.cancelled",
            aggregateType: "order",
            aggregateId: order.Id,
            payload: JsonSerializer.Serialize(new
            {
                orderId = order.Id,
                reason = request.Reason,
                notes = request.Notes,
                authorizedBy,
                stage = order.Status.ToString(),
            }),
            origin: _eventOrigin.Origin,
            occurredAt: cancelledAt,
            storeId: order.StoreId,
            actorId: actorId == Guid.Empty ? null : actorId,
            authorizedBy: authorizedBy,
            deviceId: deviceId));

        var itemResponses = new List<CancelledOrderItemResponse>();

        foreach (var (item, itemWasStarted, beforeStatus) in itemSnapshots)
        {
            _db.AuditLogs.Add(AuditLog.Create(
                item.TenantId,
                action: "ORDER_ITEM_CANCELLED",
                entity: "order_item",
                occurredAt: cancelledAt,
                storeId: order.StoreId,
                actorId: actorId == Guid.Empty ? null : actorId,
                authorizedBy: authorizedBy,
                deviceId: deviceId,
                entityId: item.Id,
                before: JsonSerializer.Serialize(new { status = beforeStatus, totalPrice = item.TotalPrice }),
                after: JsonSerializer.Serialize(new { status = "CANCELLED", totalPrice = item.TotalPrice, wasStarted = itemWasStarted }),
                reason: request.Reason));

            // EVT-010 order.item.cancelled por item — mesmo payload de CancelOrderItemCommandHandler.
            _db.DomainEvents.Add(DomainEvent.Create(
                item.TenantId,
                type: "order.item.cancelled",
                aggregateType: "order_item",
                aggregateId: item.Id,
                payload: JsonSerializer.Serialize(new
                {
                    orderItemId = item.Id,
                    orderId = order.Id,
                    reason = request.Reason,
                    notes = request.Notes,
                    authorizedBy,
                    wasStarted = itemWasStarted,
                }),
                origin: _eventOrigin.Origin,
                occurredAt: cancelledAt,
                storeId: order.StoreId,
                actorId: actorId == Guid.Empty ? null : actorId,
                authorizedBy: authorizedBy,
                deviceId: deviceId));

            if (order.Session is not null)
            {
                var productName = $"{item.Variant.Product.Name} {item.Variant.Name}".Trim();
                await _broadcaster.ItemStatusChanged(item.TenantId, order.Session.TableId, item.Id, productName, "CANCELLED", cancellationToken);
            }

            itemResponses.Add(new CancelledOrderItemResponse(
                item.Id,
                OrderItemStatusLabels.ToWireStatus(item.Status),
                cancelledAt,
                request.Reason,
                request.Notes,
                itemWasStarted,
                AuthorizedBy: null));
        }

        var authorizedBySummary = await ResolveAuthorizedBySummaryAsync(authorizedBy, cancellationToken);

        // Propaga o mesmo resumo do autorizador do pedido para cada item do envelope de resposta —
        // é a MESMA autorização (uma só chamada de /v1/auth/authorize cobre o pedido inteiro).
        itemResponses = itemResponses
            .Select(i => i with { AuthorizedBy = authorizedBySummary })
            .ToList();

        return Result<CancelOrderResponse>.Success(new CancelOrderResponse(new CancelledOrderResponse(
            order.Id,
            OrderStatusLabels.ToWireStatus(order.Status),
            cancelledAt,
            request.Reason,
            authorizedBySummary,
            itemResponses)));
    }

    private async Task<AuthorizedBySummary?> ResolveAuthorizedBySummaryAsync(Guid? authorizedBy, CancellationToken cancellationToken)
    {
        if (authorizedBy is not { } authorizedById)
        {
            return null;
        }

        var authorizer = await _db.Users.SingleOrDefaultAsync(u => u.Id == authorizedById, cancellationToken);
        return authorizer is null ? null : new AuthorizedBySummary(authorizer.Id, authorizer.Name);
    }
}
