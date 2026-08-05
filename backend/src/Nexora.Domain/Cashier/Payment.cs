using Nexora.Domain.Common;

namespace Nexora.Domain.Cashier;

/// <summary>
/// Pagamento recebido por um pedido e/ou uma sessão de mesa, podendo ser alocado entre vários
/// pedidos (<see cref="PaymentAllocation"/>) quando a conta é dividida ou paga em conjunto.
/// </summary>
public sealed class Payment
{
    private readonly List<PaymentAllocation> _allocations = new();

    private Payment() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid? SessionId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? CashSessionId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal TipAmount { get; private set; }
    public decimal ChangeAmount { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderRef { get; private set; }
    // TODO: tipar quando o formato for definido
    public string? ProviderPayload { get; private set; }
    public int Installments { get; private set; } = 1;
    public string? CardBrand { get; private set; }
    public string? AuthorizationCode { get; private set; }

    /// <summary>US-058 — ver <see cref="PaymentReconciliationStatus"/>. <c>Pending</c> sempre que há <see cref="Provider"/> (maquininha), <c>NotApplicable</c> caso contrário (ex.: dinheiro).</summary>
    public PaymentReconciliationStatus ReconciliationStatus { get; private set; } = PaymentReconciliationStatus.NotApplicable;
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? RefundedAt { get; private set; }
    public decimal? RefundAmount { get; private set; }
    public string? RefundReason { get; private set; }
    public Guid? AuthorizedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    public static Payment Create(
        Guid tenantId,
        Guid storeId,
        DateOnly businessDay,
        PaymentMethod method,
        decimal amount,
        decimal netAmount,
        decimal feeAmount = 0,
        decimal tipAmount = 0,
        decimal changeAmount = 0,
        int installments = 1,
        Guid? sessionId = null,
        Guid? orderId = null,
        Guid? cashSessionId = null,
        string? provider = null,
        string? providerRef = null,
        string? cardBrand = null,
        Guid? createdBy = null)
    {
        if (amount <= 0)
            throw new DomainException("O valor do pagamento deve ser maior que zero.");

        if (feeAmount < 0)
            throw new DomainException("A taxa do pagamento não pode ser negativa.");

        if (tipAmount < 0)
            throw new DomainException("A gorjeta do pagamento não pode ser negativa.");

        if (changeAmount < 0)
            throw new DomainException("O troco do pagamento não pode ser negativo.");

        if (installments < 1)
            throw new DomainException("O número de parcelas deve ser pelo menos 1.");

        var now = DateTimeOffset.UtcNow;

        return new Payment
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            SessionId = sessionId,
            OrderId = orderId,
            CashSessionId = cashSessionId,
            BusinessDay = businessDay,
            Method = method,
            Status = PaymentStatus.Pending,
            Amount = amount,
            FeeAmount = feeAmount,
            NetAmount = netAmount,
            TipAmount = tipAmount,
            ChangeAmount = changeAmount,
            Provider = provider,
            ProviderRef = providerRef,
            Installments = installments,
            CardBrand = cardBrand,
            ReconciliationStatus = string.IsNullOrWhiteSpace(provider)
                ? PaymentReconciliationStatus.NotApplicable
                : PaymentReconciliationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy
        };
    }

    public void MarkAuthorized(string? authorizationCode = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("Só é possível autorizar um pagamento pendente.");

        Status = PaymentStatus.Authorized;
        AuthorizationCode = authorizationCode ?? AuthorizationCode;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Refunded or PaymentStatus.Cancelled)
            throw new DomainException("Não é possível marcar como pago um pagamento nesse estado.");

        Status = PaymentStatus.Paid;
        PaidAt = paidAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Refunded)
            throw new DomainException("Não é possível marcar como falho um pagamento já pago ou estornado.");

        Status = PaymentStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is PaymentStatus.Paid or PaymentStatus.Refunded)
            throw new DomainException("Não é possível cancelar um pagamento já pago ou estornado.");

        Status = PaymentStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Refund(decimal refundAmount, DateTimeOffset refundedAt, string? refundReason, Guid? authorizedBy = null)
    {
        if (Status != PaymentStatus.Paid)
            throw new DomainException("Só é possível estornar um pagamento pago.");

        if (refundAmount <= 0 || refundAmount > Amount)
            throw new DomainException("O valor do estorno deve ser maior que zero e não pode exceder o valor pago.");

        Status = PaymentStatus.Refunded;
        RefundAmount = refundAmount;
        RefundedAt = refundedAt;
        RefundReason = refundReason;
        AuthorizedBy = authorizedBy ?? AuthorizedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal void AddAllocation(PaymentAllocation allocation)
    {
        if (allocation.PaymentId != Id)
            throw new DomainException("A alocação de pagamento não pertence a este pagamento.");

        _allocations.Add(allocation);
    }

    public PaymentAllocation AllocateTo(Guid orderId, decimal amount)
    {
        var allocation = PaymentAllocation.Create(TenantId, Id, orderId, amount);
        _allocations.Add(allocation);
        return allocation;
    }
}
