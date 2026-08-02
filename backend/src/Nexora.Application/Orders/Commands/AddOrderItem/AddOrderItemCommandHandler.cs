using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
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
    private readonly IAlertsBroadcaster _alertsBroadcaster;

    public AddOrderItemCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        IOrderConsumptionBroadcaster broadcaster,
        IAlertsBroadcaster alertsBroadcaster)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _alertsBroadcaster = alertsBroadcaster;
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

        var unitPriceResult = await ResolveCurrentPriceAsync(variant.Id, session.TenantId, cancellationToken);
        if (unitPriceResult is null)
        {
            return Result<OrderItemResponse>.Failure("Este item não tem preço vigente cadastrado.", ApiErrorCodes.OrderItemVariantPriceNotFound);
        }

        var order = await FindOrCreateOpenOrderAsync(session, cancellationToken);

        var quantity = request.Quantity < 1 ? (short)1 : request.Quantity;
        var item = OrderItem.Create(
            session.TenantId,
            order.Id,
            variant.Id,
            unitPriceResult.Value,
            quantity,
            stationId: product.StationId,
            notes: request.Notes);

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

        foreach (var fractionInput in request.Fractions ?? Array.Empty<AddOrderItemFractionInput>())
        {
            var fractionVariant = await _db.ProductVariants
                .SingleOrDefaultAsync(v => v.Id == fractionInput.VariantId && v.TenantId == session.TenantId && v.DeletedAt == null, cancellationToken);
            if (fractionVariant is null)
            {
                return Result<OrderItemResponse>.Failure("Variante da fração não encontrada.", ApiErrorCodes.VariantNotFound);
            }

            var fractionPrice = await ResolveCurrentPriceAsync(fractionVariant.Id, session.TenantId, cancellationToken);
            if (fractionPrice is null)
            {
                return Result<OrderItemResponse>.Failure("Fração sem preço vigente cadastrado.", ApiErrorCodes.OrderItemVariantPriceNotFound);
            }

            item.AddFraction(OrderItemFraction.Create(session.TenantId, item.Id, fractionVariant.Id, fractionInput.Weight, fractionPrice.Value));
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

        return Result<OrderItemResponse>.Success(OrderItemMapper.Map(item, productName));
    }

    private async Task<decimal?> ResolveCurrentPriceAsync(Guid variantId, Guid tenantId, CancellationToken cancellationToken)
    {
        var price = await _db.Prices
            .Where(p => p.VariantId == variantId && p.TenantId == tenantId && p.Channel == Channel.DineIn && p.ValidTo == null)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return price?.Amount;
    }

    /// <summary>
    /// Reaproveita um pedido ainda aberto da sessão (nem fechado, nem cancelado) em vez de criar um
    /// novo a cada item — mais próximo do comportamento real de "uma comanda acumula pedidos, cada
    /// pedido acumula itens" (Docs/Domain/03-Operacao.md, ERD). Gera um <c>short_code</c> simples
    /// (ADR-016 pede curto e único por loja+dia operacional; a geração "de verdade", com retentativa
    /// em colisão e formato amigável ao garçom, é responsabilidade de US-030 — aqui um sufixo
    /// hexadecimal de <see cref="Guid.CreateVersion7"/> já cobre a unicidade prática desta wave).
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

        var shortCode = Guid.CreateVersion7().ToString("N")[..8].ToUpperInvariant();
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
