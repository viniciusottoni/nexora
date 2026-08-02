using Nexora.Domain.Common;

namespace Nexora.Domain.Cashier;

/// <summary>
/// Fração de um pagamento alocada a um pedido específico — usado quando um único pagamento
/// quita mais de um pedido (conta conjunta) ou quando um pedido é pago em múltiplas partes.
/// </summary>
public sealed class PaymentAllocation
{
    private PaymentAllocation() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }

    public static PaymentAllocation Create(Guid tenantId, Guid paymentId, Guid orderId, decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("O valor alocado do pagamento deve ser maior que zero.");

        return new PaymentAllocation
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            PaymentId = paymentId,
            OrderId = orderId,
            Amount = amount
        };
    }
}
