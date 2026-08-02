using Nexora.Domain.Catalog;
using Nexora.Domain.Common;

namespace Nexora.Domain.Operation;

/// <summary>
/// Item de um pedido — unidade que percorre a fila de produção do KDS (<see cref="OrderItemStatus"/>).
/// Cada transição de status é a origem de uma métrica de tempo de produção (ADR-006) e precisa
/// ser gravada, na Application, na mesma transação do evento correspondente.
/// </summary>
public sealed class OrderItem
{
    private readonly List<OrderItemFraction> _fractions = new();
    private readonly List<OrderItemModifier> _modifiers = new();

    private OrderItem() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid? StationId { get; private set; }
    public short Quantity { get; private set; } = 1;
    public decimal UnitPrice { get; private set; }
    public decimal ModifiersTotal { get; private set; }
    public decimal TotalPrice { get; private set; }
    public decimal? UnitCost { get; private set; }
    public OrderItemStatus Status { get; private set; } = OrderItemStatus.Queued;
    public string? Notes { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public DateTimeOffset? FireAt { get; private set; }
    public DateTimeOffset? FiredAt { get; private set; }
    public DateTimeOffset? OvenInAt { get; private set; }
    public DateTimeOffset? OvenOutAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? ServedAt { get; private set; }
    public short? OvenSlot { get; private set; }
    public int? PriorityScore { get; private set; }
    public string? CancelReason { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public Guid? AuthorizedBy { get; private set; }
    public Guid? RefireOfId { get; private set; }
    public string? RefireReason { get; private set; }

    /// <summary>
    /// US-028 (Repetir item com um toque) — id do item original quando este item nasceu de uma
    /// repetição de um toque, nunca de um refogo de KDS (esse é <see cref="RefireOfId"/>, conceito
    /// distinto: refogo é a cozinha refazendo o MESMO pedido por erro/queda de qualidade;
    /// repetição é o cliente/garçom pedindo uma SEGUNDA unidade do mesmo item, com preço vigente
    /// no momento da repetição, não o do item original).
    /// </summary>
    public Guid? RepeatedFromItemId { get; private set; }
    public Guid? FiredBy { get; private set; }
    public Guid? ReadyBy { get; private set; }
    public Guid? ServedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Order Order { get; private set; } = null!;
    public ProductVariant Variant { get; private set; } = null!;
    public IReadOnlyCollection<OrderItemFraction> Fractions => _fractions.AsReadOnly();
    public IReadOnlyCollection<OrderItemModifier> Modifiers => _modifiers.AsReadOnly();

    public static OrderItem Create(
        Guid tenantId,
        Guid orderId,
        Guid variantId,
        decimal unitPrice,
        short quantity = 1,
        Guid? stationId = null,
        string? notes = null,
        Guid? repeatedFromItemId = null)
    {
        if (quantity < 1)
            throw new DomainException("A quantidade do item precisa ser pelo menos 1.");

        if (unitPrice < 0)
            throw new DomainException("O preço unitário não pode ser negativo.");

        var now = DateTimeOffset.UtcNow;

        var item = new OrderItem
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            OrderId = orderId,
            VariantId = variantId,
            StationId = stationId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Notes = notes,
            RepeatedFromItemId = repeatedFromItemId,
            Status = OrderItemStatus.Queued,
            PlacedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        item.RecalculateTotal();

        return item;
    }

    /// <summary>Cria um novo item de refogo referenciando o item original — nunca reabre o item já servido/cancelado.</summary>
    public static OrderItem Refire(OrderItem original, string reason, Guid firedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo do refogo é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        var refire = new OrderItem
        {
            Id = IdGenerator.NewId(),
            TenantId = original.TenantId,
            OrderId = original.OrderId,
            VariantId = original.VariantId,
            StationId = original.StationId,
            Quantity = original.Quantity,
            UnitPrice = original.UnitPrice,
            Status = OrderItemStatus.Queued,
            RefireOfId = original.Id,
            RefireReason = reason,
            FiredBy = firedBy,
            PlacedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        refire.RecalculateTotal();

        return refire;
    }

    public void AddFraction(OrderItemFraction fraction)
    {
        _fractions.Add(fraction);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddModifier(OrderItemModifier modifier)
    {
        _modifiers.Add(modifier);
        ModifiersTotal += modifier.PriceDelta * modifier.Quantity;
        RecalculateTotal();
    }

    public void ScheduleFire(DateTimeOffset fireAt)
    {
        FireAt = fireAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Fire(Guid firedBy)
    {
        if (Status is not OrderItemStatus.Queued)
            throw new DomainException("Só é possível disparar um item que está na fila.");

        Status = OrderItemStatus.Fired;
        FiredAt = DateTimeOffset.UtcNow;
        FiredBy = firedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SendToOven(short? ovenSlot)
    {
        if (Status is not OrderItemStatus.Fired)
            throw new DomainException("Só é possível levar ao forno um item já disparado.");

        Status = OrderItemStatus.InOven;
        OvenInAt = DateTimeOffset.UtcNow;
        OvenSlot = ovenSlot;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void TakeOutOfOven()
    {
        if (Status is not OrderItemStatus.InOven)
            throw new DomainException("Só é possível retirar do forno um item que está no forno.");

        Status = OrderItemStatus.OutOfOven;
        OvenOutAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReady(Guid readyBy)
    {
        if (Status is OrderItemStatus.Served or OrderItemStatus.Cancelled)
            throw new DomainException("Item servido ou cancelado não pode ficar pronto novamente.");

        Status = OrderItemStatus.Ready;
        ReadyAt = DateTimeOffset.UtcNow;
        ReadyBy = readyBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkServed(Guid servedBy)
    {
        if (Status is not OrderItemStatus.Ready)
            throw new DomainException("Só é possível servir um item pronto.");

        Status = OrderItemStatus.Served;
        ServedAt = DateTimeOffset.UtcNow;
        ServedBy = servedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason, Guid cancelledBy, Guid? authorizedBy = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo do cancelamento é obrigatório.");

        if (Status is OrderItemStatus.Served)
            throw new DomainException("Item já servido não pode ser cancelado.");

        Status = OrderItemStatus.Cancelled;
        CancelReason = reason;
        CancelledBy = cancelledBy;
        AuthorizedBy = authorizedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateUnitCost(decimal unitCost)
    {
        if (unitCost < 0)
            throw new DomainException("O custo unitário não pode ser negativo.");

        UnitCost = unitCost;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void RecalculateTotal()
    {
        // ordem de cálculo normativa (ADR-017 §"Ordem das operações"): unitário × quantidade, + modificadores
        TotalPrice = (UnitPrice * Quantity) + ModifiersTotal;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
