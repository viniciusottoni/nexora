using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Commands.AddOrderItem;

/// <summary>
/// Capacidade MÍNIMA de "lançar item no pedido de uma sessão" — construída para preencher uma
/// lacuna real do backlog: as histórias desta wave (US-024 "Consumo da mesa em tempo real" e
/// US-028 "Repetir item com um toque") dependem, na especificação original, de US-030 ("Enviar
/// pedido pelo cardápio", épico E-03, Pedido e Roteamento) — que não é parte de E-02 e não foi
/// implementada (nem aqui, nem em nenhum outro worktree paralelo confirmado por busca no
/// histórico). Sem ALGUM jeito de colocar um item no pedido de uma mesa, US-024 não teria itens de
/// verdade para listar e US-028 não teria nada para repetir.
///
/// [DECISÃO DE ESCOPO] Este comando NÃO é o fluxo completo de "montar pedido pelo cardápio" — não
/// há carrinho, não há tela de composição de fração na UI, não há descoberta de disponibilidade de
/// modificador por grupo. Ele aceita os dados já resolvidos (variantId/quantity/notes/modifiers/
/// fractions) e faz o mínimo de validação de negócio (disponibilidade do produto, preço vigente,
/// sessão aberta) para gerar um <see cref="OrderItem"/> real e consistente. O fluxo completo de
/// carrinho — incluindo montagem de fração na tela, sugestão de modificador, etc. — continua sendo
/// US-030, fora do escopo desta tarefa.
/// </summary>
internal sealed class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand, Result<OrderItemResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IOrderConsumptionBroadcaster _broadcaster;
    private readonly IStationBroadcaster _stationBroadcaster;
    private readonly IAlertsBroadcaster _alertsBroadcaster;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IOrderShortCodeAllocator _shortCodeAllocator;

    public AddOrderItemCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        IStationBroadcaster stationBroadcaster,
        IAlertsBroadcaster alertsBroadcaster,
        ICurrentTenantContext tenantContext,
        IOrderShortCodeAllocator shortCodeAllocator)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _stationBroadcaster = stationBroadcaster;
        _alertsBroadcaster = alertsBroadcaster;
        _tenantContext = tenantContext;
        _shortCodeAllocator = shortCodeAllocator;
    }

    public async Task<Result<OrderItemResponse>> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions
            .Include(s => s.Table)
            .SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
        {
            return Result<OrderItemResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
        {
            return Result<OrderItemResponse>.Failure("Só é possível lançar item em uma comanda aberta.", ApiErrorCodes.TableSessionNotOpen);
        }

        // US-026 §4, cenário "Novo pedido após solicitar a conta": um item novo devolve a sessão
        // para OPEN e avisa o caixa — silencioso para o cliente (não é erro, só segue o fluxo
        // normal de adicionar item), explícito para quem estava preparando a conta.
        var wasBillRequested = session.Status == TableSessionStatus.BillRequested;

        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .SingleOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == session.TenantId && v.DeletedAt == null, cancellationToken);

        if (variant is null)
        {
            return Result<OrderItemResponse>.Failure("Variante não encontrada.", ApiErrorCodes.VariantNotFound);
        }

        var product = variant.Product;
        if (!product.IsActive || !product.IsAvailable)
        {
            return Result<OrderItemResponse>.Failure(
                product.UnavailableReason is { Length: > 0 } reason
                    ? $"Este produto está indisponível: {reason}."
                    : "Este produto está indisponível no momento.",
                ApiErrorCodes.ProductUnavailable);
        }

        // US-030 §5, RN "grupo respeita mínimo/máximo/obrigatório de seleção" — mesma validação
        // usada por CreateOrderCommand/AddItemToOrderCommand (Nexora.Application.Orders.Support.ModifierGroupValidator).
        var groupSpecs = await _db.ProductModifierGroups
            .Where(pmg => pmg.ProductId == product.Id && pmg.TenantId == session.TenantId)
            .Select(pmg => new ModifierGroupValidator.GroupSpec(
                pmg.Group.Id,
                pmg.Group.Name,
                pmg.Group.MinSelect,
                pmg.Group.MaxSelect,
                pmg.Group.IsRequired,
                pmg.Group.Modifiers.Where(m => m.DeletedAt == null).Select(m => m.Id).ToList()))
            .ToListAsync(cancellationToken);

        var selectedModifierIds = (request.Modifiers ?? Array.Empty<AddOrderItemModifierInput>())
            .Select(m => m.ModifierId).ToList();

        var groupViolation = ModifierGroupValidator.ValidateAll(groupSpecs, selectedModifierIds);
        if (groupViolation is not null)
        {
            return Result<OrderItemResponse>.Failure(
                "Escolha pendente em um grupo de modificadores.",
                groupViolation.Code,
                new Dictionary<string, string[]>
                {
                    ["groupId"] = new[] { groupViolation.GroupId.ToString() },
                    ["groupName"] = new[] { groupViolation.GroupName },
                });
        }

        // US-030 §4, cenário "Preço aplicado por canal" — resolve pelo canal REAL do pedido (a
        // sessão de mesa é sempre DineIn, mas o cálculo já reaproveita a mesma herança de canal do
        // resto da solution, em vez de fixar Channel.DineIn direto na query).
        var order = await FindOrCreateOpenOrderAsync(session, cancellationToken);

        var tenantConfig = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == session.TenantId, cancellationToken);

        // US-013/US-030: item com fração usa o preço calculado pela regra do tenant, nunca o
        // preço da variante "molde" enviada em variantId — ver OrderItemFractionPricing.
        var pricing = await OrderItemFractionPricing.ResolveAsync(
            _db, session.TenantId, order.Channel, variant.Id, request.Fractions, tenantConfig?.Operation, cancellationToken);
        if (pricing.IsFailure)
        {
            return Result<OrderItemResponse>.Failure(pricing.Error!, pricing.Code, pricing.Errors);
        }

        var quantity = request.Quantity < 1 ? (short)1 : request.Quantity;
        var item = OrderItem.Create(
            session.TenantId,
            order.Id,
            variant.Id,
            pricing.Value!.UnitPrice,
            quantity,
            stationId: product.StationId,
            notes: request.Notes,
            deviceId: _tenantContext.DeviceId);

        foreach (var modifierInput in request.Modifiers ?? Array.Empty<AddOrderItemModifierInput>())
        {
            var modifier = await _db.Modifiers
                .SingleOrDefaultAsync(m => m.Id == modifierInput.ModifierId && m.TenantId == session.TenantId && m.DeletedAt == null, cancellationToken);
            if (modifier is null)
            {
                return Result<OrderItemResponse>.Failure("Modificador não encontrado.", ApiErrorCodes.ModifierNotFound);
            }

            item.AddModifier(OrderItemModifier.Create(
                session.TenantId, item.Id, modifier.Id, modifier.Name, modifier.PriceDelta, modifierInput.Quantity < 1 ? (short)1 : modifierInput.Quantity));
        }

        foreach (var fraction in pricing.Value.Fractions)
        {
            item.AddFraction(OrderItemFraction.Create(session.TenantId, item.Id, fraction.Variant.Id, fraction.Weight, fraction.UnitPrice));
        }

        order.AddItem(item);

        var now = DateTimeOffset.UtcNow;
        _db.AuditLogs.Add(AuditLog.Create(
            session.TenantId,
            action: "ORDER_ITEM_ADDED",
            entity: "order_item",
            occurredAt: now,
            storeId: session.StoreId,
            entityId: item.Id,
            after: JsonSerializer.Serialize(new { orderId = order.Id, sessionId = session.Id, variantId = variant.Id, quantity = item.Quantity })));

        // EVT-003 order.item.added (US-028 §6) — mesmo evento geral de "item lançado", reaproveitado
        // pela repetição (repeatedFrom nulo aqui, preenchido em RepeatOrderItemCommandHandler).
        _db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "order.item.added",
            aggregateType: "order_item",
            aggregateId: item.Id,
            payload: JsonSerializer.Serialize(new
            {
                variantId = item.VariantId,
                qty = item.Quantity,
                modifiers = item.Modifiers.Select(m => new { m.ModifierId, m.Quantity }),
                fractions = item.Fractions.Select(f => new { f.VariantId, f.Weight }),
                repeatedFrom = (Guid?)null,
            }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            storeId: session.StoreId));

        var productName = $"{product.Name} {variant.Name}".Trim();

        if (wasBillRequested)
        {
            session.ReopenAfterNewItem();

            _db.AuditLogs.Add(AuditLog.Create(
                session.TenantId,
                action: "TABLE_SESSION_REOPENED_AFTER_NEW_ITEM",
                entity: "table_session",
                occurredAt: now,
                storeId: session.StoreId,
                entityId: session.Id,
                after: JsonSerializer.Serialize(new { orderItemId = item.Id })));

            _db.DomainEvents.Add(DomainEvent.Create(
                session.TenantId,
                type: "table.session.reopened",
                aggregateType: "table_session",
                aggregateId: session.Id,
                payload: JsonSerializer.Serialize(new { tableId = session.TableId, orderItemId = item.Id }),
                origin: _eventOrigin.Origin,
                occurredAt: now,
                storeId: session.StoreId));

            // US-026 §4: "o caixa deve ser informado da mudança" — a conta que estava sendo
            // preparada não vale mais, o caixa precisa saber ANTES de fechar com base nela.
            await _alertsBroadcaster.BillRequestCancelled(session.TenantId, session.TableId, session.Table.Label, cancellationToken);
        }

        // Broadcast síncrono, aguardado dentro do handler (mesmo padrão de MarkProductUnavailableCommandHandler/IAvailabilityBroadcaster).
        await _broadcaster.ItemAdded(session.TenantId, session.TableId, item.Id, productName, repeatedFromItemId: null, cancellationToken);

        // US-031 (Roteamento simultâneo para cozinha e caixa) — item lançado depois do pedido já
        // criado (comanda em aberto) nasce QUEUED e precisa do MESMO roteamento por praça de
        // CreateOrderCommandHandler, só que para um item isolado (EVT-004 order.item.queued).
        await _stationBroadcaster.ItemQueued(
            session.TenantId,
            order.Id,
            order.ShortCode,
            session.TableId,
            session.Table.Label,
            order.Channel.ToString(),
            new StationBroadcastItem(
                item.Id, productName, item.StationId, item.Quantity, item.Modifiers.Select(m => m.NameSnapshot).ToArray(), item.Notes),
            now,
            cancellationToken);

        return Result<OrderItemResponse>.Success(OrderItemMapper.Map(item, productName));
    }

    /// <summary>
    /// Reaproveita um pedido ainda aberto da sessão (nem fechado, nem cancelado) em vez de criar um
    /// novo a cada item — mais próximo do comportamento real de "uma comanda acumula pedidos, cada
    /// pedido acumula itens" (Docs/Domain/03-Operacao.md, ERD). <c>short_code</c> gerado por
    /// <see cref="IOrderShortCodeAllocator"/> (US-030 §8/ADR-016: formato <c>{letra}{sequência}</c>
    /// único por loja+dia operacional, com lock consultivo do Postgres — não mais o sufixo
    /// hexadecimal provisório de <see cref="Guid.CreateVersion7"/> desta wave anterior).
    /// </summary>
    private async Task<Order> FindOrCreateOpenOrderAsync(TableSession session, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Where(o => o.SessionId == session.Id && o.Status != OrderStatus.Closed && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is not null)
        {
            return order;
        }

        var shortCode = await _shortCodeAllocator.AllocateAsync(session.StoreId, session.BusinessDay, cancellationToken);
        order = Order.Create(
            session.TenantId,
            session.StoreId,
            Channel.DineIn,
            shortCode,
            session.BusinessDay,
            sessionId: session.Id);

        order.Place();

        _db.Orders.Add(order);
        return order;
    }
}
