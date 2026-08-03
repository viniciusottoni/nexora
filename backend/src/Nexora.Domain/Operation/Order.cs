using Nexora.Domain.Catalog;
using Nexora.Domain.Common;

namespace Nexora.Domain.Operation;

/// <summary>
/// Pedido — agregado central da operação. Toda transição de <see cref="OrderStatus"/> é a
/// origem de uma métrica de tempo (ADR-006, KDS em menos de 2s) e precisa ser gravada, na
/// Application, na mesma transação do evento correspondente.
/// </summary>
public sealed class Order
{
    private readonly List<OrderItem> _items = new();

    private Order() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid? SessionId { get; private set; }
    public Channel Channel { get; private set; }
    public string ShortCode { get; private set; } = string.Empty;
    public DateOnly BusinessDay { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public Guid? CustomerId { get; private set; }
    public Guid? AddressId { get; private set; }
    public Guid? CourierId { get; private set; }
    public DateTimeOffset? PlacedAt { get; private set; }
    public DateTimeOffset? FirstFiredAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? ServedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? PromisedAt { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal Total { get; private set; }
    public string? Notes { get; private set; }
    public string? CancelReason { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public Guid? AuthorizedBy { get; private set; }
    public FiscalStatus FiscalStatus { get; private set; } = FiscalStatus.None;
    public string? FiscalKey { get; private set; }
    public int? FiscalNumber { get; private set; }
    public short? FiscalSeries { get; private set; }
    public string? FiscalProtocol { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? DeviceId { get; private set; }

    public TableSession? Session { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(
        Guid tenantId,
        Guid storeId,
        Channel channel,
        string shortCode,
        DateOnly businessDay,
        Guid? sessionId = null,
        Guid? customerId = null,
        Guid? addressId = null,
        Guid? createdBy = null,
        Guid? deviceId = null)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            throw new DomainException("O código curto do pedido é obrigatório.");

        if (shortCode.Length > 8)
            throw new DomainException("O código curto do pedido não pode ter mais de 8 caracteres.");

        var now = DateTimeOffset.UtcNow;

        return new Order
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            SessionId = sessionId,
            Channel = channel,
            ShortCode = shortCode,
            BusinessDay = businessDay,
            Status = OrderStatus.Draft,
            CustomerId = customerId,
            AddressId = addressId,
            CreatedBy = createdBy,
            DeviceId = deviceId,
            FiscalStatus = FiscalStatus.None,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <param name="occurredAt">
    /// US-030/ADR-034 — horário real da confirmação (T0), já corrigido pelo desvio de relógio do
    /// dispositivo quando aplicável (ver <see cref="Nexora.Application.Orders.Support.ClockSkewPolicy"/>).
    /// Nulo usa o relógio do servidor no momento da chamada — mesma convenção de
    /// <see cref="OrderItem.Fire"/>/<see cref="TableSession.Create"/>.
    /// </param>
    public void Place(DateTimeOffset? occurredAt = null)
    {
        if (Status is not OrderStatus.Draft)
            throw new DomainException("Só é possível confirmar um pedido em rascunho.");

        Status = OrderStatus.Placed;
        PlacedAt = occurredAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void StartProduction()
    {
        if (Status is not OrderStatus.Placed)
            throw new DomainException("Só é possível iniciar a produção de um pedido confirmado.");

        Status = OrderStatus.InProduction;
        FirstFiredAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReady()
    {
        if (Status is not OrderStatus.InProduction)
            throw new DomainException("Só é possível concluir a produção de um pedido em produção.");

        Status = OrderStatus.Ready;
        ReadyAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Dispatch(Guid? courierId = null)
    {
        if (Status is not OrderStatus.Ready)
            throw new DomainException("Só é possível despachar um pedido pronto.");

        if (Channel is not Channel.Delivery)
            throw new DomainException("Somente pedidos de delivery são despachados.");

        Status = OrderStatus.Dispatched;
        CourierId = courierId ?? CourierId;
        DispatchedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDelivered()
    {
        if (Status is not OrderStatus.Dispatched)
            throw new DomainException("Só é possível marcar como entregue um pedido despachado.");

        Status = OrderStatus.Delivered;
        ServedAt ??= DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Marca o momento em que um pedido de salão (dine-in) foi servido à mesa — não altera o status.</summary>
    public void MarkServed()
    {
        if (Status is not (OrderStatus.Ready or OrderStatus.InProduction))
            throw new DomainException("Só é possível servir um pedido pronto ou em produção.");

        ServedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (Status is not (OrderStatus.Ready or OrderStatus.Delivered))
            throw new DomainException("Só é possível fechar um pedido pronto ou entregue.");

        Status = OrderStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason, Guid cancelledBy, Guid? authorizedBy = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("O motivo do cancelamento é obrigatório.");

        if (Status is OrderStatus.Closed or OrderStatus.Cancelled)
            throw new DomainException("Pedido fechado ou já cancelado não pode ser cancelado novamente.");

        Status = OrderStatus.Cancelled;
        CancelReason = reason;
        CancelledBy = cancelledBy;
        AuthorizedBy = authorizedBy;
        CancelledAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateTotals(decimal subtotal, decimal discountAmount, decimal deliveryFee, decimal serviceFeeAmount, decimal total)
    {
        if (subtotal < 0 || discountAmount < 0 || deliveryFee < 0 || serviceFeeAmount < 0 || total < 0)
            throw new DomainException("Valores do pedido não podem ser negativos.");

        Subtotal = subtotal;
        DiscountAmount = discountAmount;
        DeliveryFee = deliveryFee;
        ServiceFeeAmount = serviceFeeAmount;
        Total = total;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPromisedAt(DateTimeOffset promisedAt)
    {
        PromisedAt = promisedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFiscalPending()
    {
        FiscalStatus = FiscalStatus.Pending;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFiscalIssued(string fiscalKey, int fiscalNumber, short fiscalSeries, string? fiscalProtocol)
    {
        if (string.IsNullOrWhiteSpace(fiscalKey))
            throw new DomainException("A chave fiscal é obrigatória para marcar como emitida.");

        FiscalStatus = FiscalStatus.Issued;
        FiscalKey = fiscalKey;
        FiscalNumber = fiscalNumber;
        FiscalSeries = fiscalSeries;
        FiscalProtocol = fiscalProtocol;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFiscalRejected()
    {
        FiscalStatus = FiscalStatus.Rejected;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFiscalCancelled()
    {
        FiscalStatus = FiscalStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
