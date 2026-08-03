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

    /// <summary>US-032 (Carimbos de tempo T0 a T5) — autor de T2 (entrada no gargalo), faltava até esta história.</summary>
    public Guid? OvenInBy { get; private set; }

    /// <summary>US-032 — autor de T3 (saída do gargalo), faltava até esta história.</summary>
    public Guid? OvenOutBy { get; private set; }
    public Guid? ReadyBy { get; private set; }
    public Guid? ServedBy { get; private set; }

    /// <summary>
    /// US-032 §5 (RN-004 "toda ação registra autor, horário e dispositivo") — dispositivo de
    /// origem de cada um dos seis carimbos T0 a T5. Todos opcionais (mesma convenção nula de
    /// <see cref="Order.DeviceId"/>): nem todo caminho de código tem um dispositivo identificado
    /// hoje (ex.: jobs internos, migração de dado, testes), e um <c>Guid</c> obrigatório forçaria
    /// todo chamador existente a inventar um valor sem sentido em vez de simplesmente omitir.
    /// </summary>
    public Guid? PlacedDeviceId { get; private set; }
    public Guid? FiredDeviceId { get; private set; }
    public Guid? OvenInDeviceId { get; private set; }
    public Guid? OvenOutDeviceId { get; private set; }
    public Guid? ReadyDeviceId { get; private set; }
    public Guid? ServedDeviceId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Order Order { get; private set; } = null!;
    public ProductVariant Variant { get; private set; } = null!;
    public IReadOnlyCollection<OrderItemFraction> Fractions => _fractions.AsReadOnly();
    public IReadOnlyCollection<OrderItemModifier> Modifiers => _modifiers.AsReadOnly();

    /// <param name="occurredAt">
    /// US-030/ADR-034 — horário real de nascimento do item (T0), já corrigido pelo desvio de
    /// relógio do dispositivo quando aplicável (ver
    /// <see cref="Nexora.Application.Orders.Support.ClockSkewPolicy"/>). Nulo usa o relógio do
    /// servidor no momento da chamada — mesma convenção de <see cref="Fire"/>/<see cref="Order.Place"/>,
    /// o caso comum de quem não tem o horário de origem disponível (ex.: US-024/US-028, que criam
    /// item sem preservar um horário de dispositivo offline).
    /// </param>
    public static OrderItem Create(
        Guid tenantId,
        Guid orderId,
        Guid variantId,
        decimal unitPrice,
        short quantity = 1,
        Guid? stationId = null,
        string? notes = null,
        Guid? repeatedFromItemId = null,
        Guid? deviceId = null,
        DateTimeOffset? occurredAt = null)
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
            PlacedAt = occurredAt ?? now,
            PlacedDeviceId = deviceId,
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

    /// <param name="occurredAt">
    /// US-032/ADR-034 — horário real da transição (T1), já corrigido pelo desvio de relógio do
    /// dispositivo quando aplicável. Nulo usa o relógio do servidor no momento da chamada (mesma
    /// convenção de <see cref="TableSession.Create"/>) — o caso de quem não tem o horário de
    /// origem disponível (ex.: job interno, teste).
    /// </param>
    /// <param name="deviceId">
    /// Dispositivo de origem da transição (RN-004). Opcional (mesma convenção nula de
    /// <see cref="Order.DeviceId"/>) — decisão documentada no relatório da US-032: tornar
    /// obrigatório quebraria os chamadores/testes que hoje não têm um dispositivo para informar.
    /// </param>
    public void Fire(Guid firedBy, DateTimeOffset? occurredAt = null, Guid? deviceId = null)
    {
        if (Status is not OrderItemStatus.Queued)
            throw new DomainException("Só é possível disparar um item que está na fila.");

        Status = OrderItemStatus.Fired;
        FiredAt = occurredAt ?? DateTimeOffset.UtcNow;
        FiredBy = firedBy;
        FiredDeviceId = deviceId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <param name="ovenInBy">Autor de T2 (US-032) — opcional pelo mesmo motivo de <paramref name="deviceId"/>.</param>
    /// <param name="occurredAt">Ver docstring de <see cref="Fire"/>.</param>
    /// <param name="deviceId">Ver docstring de <see cref="Fire"/>.</param>
    public void SendToOven(short? ovenSlot, Guid? ovenInBy = null, DateTimeOffset? occurredAt = null, Guid? deviceId = null)
    {
        if (Status is not OrderItemStatus.Fired)
            throw new DomainException("Só é possível levar ao forno um item já disparado.");

        Status = OrderItemStatus.InOven;
        OvenInAt = occurredAt ?? DateTimeOffset.UtcNow;
        OvenSlot = ovenSlot;
        OvenInBy = ovenInBy;
        OvenInDeviceId = deviceId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <param name="ovenOutBy">Autor de T3 (US-032) — opcional pelo mesmo motivo de <paramref name="deviceId"/>.</param>
    /// <param name="occurredAt">Ver docstring de <see cref="Fire"/>.</param>
    /// <param name="deviceId">Ver docstring de <see cref="Fire"/>.</param>
    public void TakeOutOfOven(Guid? ovenOutBy = null, DateTimeOffset? occurredAt = null, Guid? deviceId = null)
    {
        if (Status is not OrderItemStatus.InOven)
            throw new DomainException("Só é possível retirar do forno um item que está no forno.");

        Status = OrderItemStatus.OutOfOven;
        OvenOutAt = occurredAt ?? DateTimeOffset.UtcNow;
        OvenOutBy = ovenOutBy;
        OvenOutDeviceId = deviceId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <param name="occurredAt">Ver docstring de <see cref="Fire"/>.</param>
    /// <param name="deviceId">Ver docstring de <see cref="Fire"/>.</param>
    public void MarkReady(Guid readyBy, DateTimeOffset? occurredAt = null, Guid? deviceId = null)
    {
        if (Status is OrderItemStatus.Served or OrderItemStatus.Cancelled)
            throw new DomainException("Item servido ou cancelado não pode ficar pronto novamente.");

        Status = OrderItemStatus.Ready;
        ReadyAt = occurredAt ?? DateTimeOffset.UtcNow;
        ReadyBy = readyBy;
        ReadyDeviceId = deviceId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <param name="occurredAt">Ver docstring de <see cref="Fire"/>.</param>
    /// <param name="deviceId">Ver docstring de <see cref="Fire"/>.</param>
    public void MarkServed(Guid servedBy, DateTimeOffset? occurredAt = null, Guid? deviceId = null)
    {
        if (Status is not OrderItemStatus.Ready)
            throw new DomainException("Só é possível servir um item pronto.");

        Status = OrderItemStatus.Served;
        ServedAt = occurredAt ?? DateTimeOffset.UtcNow;
        ServedBy = servedBy;
        ServedDeviceId = deviceId;
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
