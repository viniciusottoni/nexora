using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Shared;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Auth;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Commands.CancelOrderItem;

/// <summary>
/// US-033 (Cancelar item ou pedido com autorização) §4/§7 — fecha o gap confirmado pelo relatório
/// de US-030/US-032: <see cref="OrderItem.Cancel"/> já existe no domínio desde antes, mas nenhum
/// comando de Application/endpoint o chamava em produção (nenhum <c>order.item.cancelled</c> era
/// emitido de verdade). Este handler só ORQUESTRA — nenhuma regra de estado nova no domínio.
///
/// Autorização elevada (ADR-023): <c>wasStarted</c> — <see cref="OrderCancellationPolicy"/> — é
/// calculado a partir do <see cref="OrderItem.Status"/> ANTES de <see cref="OrderItem.Cancel"/>
/// mutar o agregado. Quando exigida, o header <c>X-Authorization-Token</c> é validado através do
/// MESMO <see cref="IAuthorizationTokenValidator"/> que <c>[RequiresAuthorizationToken]</c> usaria
/// (nenhuma checagem de assinatura/expiração/ação reimplementada) — só que chamado aqui, no
/// handler, porque a exigência depende do estado do item (ver docstring de
/// <see cref="CancelOrderItemCommand"/>). Além disso, este handler valida o CONTEXTO do token
/// (<see cref="AuthorizationContextHasher"/>, mesmo hash calculado por
/// <c>AuthorizeSensitiveActionCommandHandler</c> ao emitir): um token emitido para autorizar o
/// cancelamento do item X nunca autoriza o item Y (ADR-023, "Autorizar o cancelamento do item X
/// não autoriza cancelar o item Y") — o validador genérico não verifica isso porque não conhece o
/// contexto de negócio de quem o chama.
///
/// RN-008 (Fase 2, US-105, fora de escopo): quando <c>wasStarted</c> é verdadeiro, o insumo já
/// consumido pela produção NÃO é estornado — deveria gerar um registro de perda
/// (<c>stock_movement</c>, <c>type=WASTE</c>). Esta história só MARCA a intenção no payload do
/// evento (<c>wasStarted: true</c>) — a baixa de estoque real é US-105.
/// </summary>
internal sealed class CancelOrderItemCommandHandler : IRequestHandler<CancelOrderItemCommand, Result<CancelOrderItemResponse>>
{
    /// <summary>Mesma chave de <c>SensitiveActionCatalog.ActionPermissions["CANCEL_STARTED_ITEM"]</c> (ADR-023) — literal aqui para não expor o catálogo (internal ao módulo Auth) fora dele.</summary>
    internal const string CancelStartedItemAction = "CANCEL_STARTED_ITEM";

    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IAuthorizationTokenValidator _authorizationTokenValidator;

    public CancelOrderItemCommandHandler(
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

    public async Task<Result<CancelOrderItemResponse>> Handle(CancelOrderItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.OrderItems
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Order).ThenInclude(o => o.Session)
            .SingleOrDefaultAsync(i => i.Id == request.ItemId && i.OrderId == request.OrderId, cancellationToken);

        if (item is null)
        {
            return Result<CancelOrderItemResponse>.Failure("Item não encontrado.", ApiErrorCodes.OrderItemNotFound);
        }

        // US-033 §4, cenário "Pedido fechado não cancela" — a mesma guarda vale para cancelar um
        // ITEM de um pedido já fechado (senão o total fechado do salão ficaria incoerente com um
        // item cancelado depois do fato); orientação aponta o fluxo de estorno (RF-CXA-13/Fase 2).
        if (item.Order.Status is OrderStatus.Closed or OrderStatus.Cancelled)
        {
            return Result<CancelOrderItemResponse>.Failure(
                "Pedido fechado ou cancelado não pode ter item cancelado — utilize o fluxo de estorno.",
                ApiErrorCodes.InvalidStateTransition);
        }

        if (item.Status is OrderItemStatus.Served or OrderItemStatus.Cancelled)
        {
            return Result<CancelOrderItemResponse>.Failure(
                item.Status is OrderItemStatus.Served
                    ? "Item já servido não pode ser cancelado."
                    : "Item já foi cancelado.",
                ApiErrorCodes.InvalidStateTransition);
        }

        var wasStarted = OrderCancellationPolicy.RequiresAuthorization(item.Status);
        var actorId = _tenantContext.UserId ?? Guid.Empty;
        var deviceId = _tenantContext.DeviceId;
        var now = DateTimeOffset.UtcNow;

        Guid? authorizedBy = null;

        if (wasStarted)
        {
            var contextHash = AuthorizationContextHasher.Hash(
                new Dictionary<string, object?> { ["orderItemId"] = item.Id.ToString() });
            var grantResult = await _authorizationTokenValidator.ValidateAsync(request.AuthorizationToken, CancelStartedItemAction, cancellationToken);

            // ADR-023: "Autorizar o cancelamento do item X não autoriza cancelar o item Y" — o
            // validador genérico só confere assinatura/expiração/ação; o vínculo ao CONTEXTO (qual
            // item) é uma regra de negócio que só este handler conhece, então é verificada aqui.
            if (grantResult.IsFailure || !string.Equals(grantResult.Value!.ContextHash, contextHash, StringComparison.Ordinal))
            {
                // IPersistsStateOnFailureCommand (ver docstring da interface): a TransactionBehavior
                // só COMITA a transação em caso de falha — ela não chama SaveChangesAsync por si só,
                // então o handler precisa gravar explicitamente antes de devolver a falha.
                LogDeniedAttempt(item, actorId, deviceId, now);
                await _db.SaveChangesAsync(cancellationToken);
                return Denied(item);
            }

            authorizedBy = grantResult.Value.AuthorizedBy;
        }

        var beforeSnapshot = JsonSerializer.Serialize(new
        {
            status = OrderItemStatusLabels.ToWireStatus(item.Status),
            totalPrice = item.TotalPrice,
        });

        item.Cancel(request.Reason, actorId, authorizedBy);
        var cancelledAt = item.UpdatedAt;

        // EVT-010 order.item.cancelled (US-033 §6) — payload exigido: reason, authorizedBy,
        // wasStarted. RN-008/US-105: wasStarted=true é a única sinalização desta história para o
        // registro de perda de estoque — a baixa real fica para US-105 (Fase 2, fora de escopo).
        // Criado antes do AuditLog para correlacionar via DomainEventId (E-09/US-090).
        var itemCancelledEvent = DomainEvent.Create(
            item.TenantId,
            type: "order.item.cancelled",
            aggregateType: "order_item",
            aggregateId: item.Id,
            payload: JsonSerializer.Serialize(new
            {
                orderItemId = item.Id,
                orderId = item.OrderId,
                reason = request.Reason,
                notes = request.Notes,
                authorizedBy,
                wasStarted,
            }),
            origin: _eventOrigin.Origin,
            occurredAt: cancelledAt,
            storeId: item.Order.StoreId,
            actorId: actorId == Guid.Empty ? null : actorId,
            authorizedBy: authorizedBy,
            deviceId: deviceId);
        _db.DomainEvents.Add(itemCancelledEvent);

        _db.AuditLogs.Add(AuditLog.Create(
            item.TenantId,
            action: "ORDER_ITEM_CANCELLED",
            entity: "order_item",
            occurredAt: cancelledAt,
            storeId: item.Order.StoreId,
            actorId: actorId == Guid.Empty ? null : actorId,
            authorizedBy: authorizedBy,
            deviceId: deviceId,
            entityId: item.Id,
            before: beforeSnapshot,
            after: JsonSerializer.Serialize(new { status = "CANCELLED", totalPrice = item.TotalPrice, wasStarted }),
            reason: request.Reason,
            domainEventId: itemCancelledEvent.Id));

        var productName = $"{item.Variant.Product.Name} {item.Variant.Name}".Trim();

        if (item.Order.Session is not null)
        {
            // US-024 (mesa em tempo real) — reaproveita ItemStatusChanged com status "CANCELLED"
            // (já suportado por OrderItemStatusLabels.ToWireStatus/ToRealtimeEventType) em vez de
            // criar um método novo na interface: sem alterar IOrderConsumptionBroadcaster nem a
            // implementação SignalR/Hub (fora do limite de arquivo desta história — a sala/fila
            // por praça de US-031 é responsabilidade de outro agente em paralelo).
            await _broadcaster.ItemStatusChanged(item.TenantId, item.Order.Session.TableId, item.Id, productName, "CANCELLED", cancellationToken);
        }

        var authorizedBySummary = await ResolveAuthorizedBySummaryAsync(authorizedBy, cancellationToken);

        return Result<CancelOrderItemResponse>.Success(new CancelOrderItemResponse(new CancelledOrderItemResponse(
            item.Id,
            OrderItemStatusLabels.ToWireStatus(item.Status),
            cancelledAt,
            request.Reason,
            request.Notes,
            wasStarted,
            authorizedBySummary)));
    }

    /// <summary>
    /// 403 <see cref="ApiErrorCodes.AuthorizationRequired"/>, contrato exato da US-033 §7:
    /// <c>meta: { action, itemStatus }</c>. RNF-SEG-15: nunca distingue ao cliente se a causa foi
    /// token ausente, inválido/expirado, de outra ação, ou de outro contexto (item).
    /// </summary>
    private static Result<CancelOrderItemResponse> Denied(OrderItem item) =>
        Result<CancelOrderItemResponse>.Failure(
            "Item já iniciado. É necessária autorização de perfil superior.",
            ApiErrorCodes.AuthorizationRequired,
            new Dictionary<string, string[]>
            {
                ["action"] = new[] { CancelStartedItemAction },
                ["itemStatus"] = new[] { OrderItemStatusLabels.ToWireStatus(item.Status) },
            });

    /// <summary>US-033 §4, cenário "Autorização negada" — a TENTATIVA de cancelamento recusada também é auditada, não só a autorização em si (que já é auditada por <c>AuthorizeSensitiveActionCommandHandler</c>).</summary>
    private void LogDeniedAttempt(OrderItem item, Guid actorId, Guid? deviceId, DateTimeOffset now)
    {
        _db.AuditLogs.Add(AuditLog.Create(
            item.TenantId,
            action: "ORDER_ITEM_CANCEL_DENIED",
            entity: "order_item",
            occurredAt: now,
            storeId: item.Order.StoreId,
            actorId: actorId == Guid.Empty ? null : actorId,
            deviceId: deviceId,
            entityId: item.Id,
            after: JsonSerializer.Serialize(new { requiredAction = CancelStartedItemAction, itemStatus = OrderItemStatusLabels.ToWireStatus(item.Status) })));
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
