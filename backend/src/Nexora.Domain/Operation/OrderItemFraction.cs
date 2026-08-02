using Nexora.Domain.Catalog;
using Nexora.Domain.Common;

namespace Nexora.Domain.Operation;

/// <summary>
/// Fração de um item de pedido — usada em produtos "meio a meio". A soma das frações de um
/// item deve fechar em 1 (validado na Application; aqui só se garante que cada fração isolada
/// é um número válido entre 0 e 1 — ADR-017, ordem de cálculo do preço).
/// </summary>
public sealed class OrderItemFraction
{
    private OrderItemFraction() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid VariantId { get; private set; }
    public decimal Weight { get; private set; }
    public decimal UnitPrice { get; private set; }
    public short SortOrder { get; private set; }

    public OrderItem Item { get; private set; } = null!;
    public ProductVariant Variant { get; private set; } = null!;

    public static OrderItemFraction Create(Guid tenantId, Guid orderItemId, Guid variantId, decimal weight, decimal unitPrice, short sortOrder = 0)
    {
        if (weight <= 0 || weight > 1)
            throw new DomainException("O peso da fração precisa estar entre 0 (exclusivo) e 1.");

        if (unitPrice < 0)
            throw new DomainException("O preço da fração não pode ser negativo.");

        return new OrderItemFraction
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            OrderItemId = orderItemId,
            VariantId = variantId,
            Weight = weight,
            UnitPrice = unitPrice,
            SortOrder = sortOrder
        };
    }
}
