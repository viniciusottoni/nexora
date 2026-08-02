using Nexora.Domain.Common;

namespace Nexora.Domain.Operation;

/// <summary>
/// Modificador selecionado em um item de pedido — captura um "snapshot" do nome e do preço
/// do <see cref="Catalog.Modifier"/> no momento do pedido, para não ser afetado por mudança
/// posterior do cardápio.
/// </summary>
public sealed class OrderItemModifier
{
    private OrderItemModifier() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid ModifierId { get; private set; }
    public short Quantity { get; private set; } = 1;
    public decimal PriceDelta { get; private set; }
    public string NameSnapshot { get; private set; } = string.Empty;

    public OrderItem Item { get; private set; } = null!;

    public static OrderItemModifier Create(Guid tenantId, Guid orderItemId, Guid modifierId, string nameSnapshot, decimal priceDelta = 0m, short quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(nameSnapshot))
            throw new DomainException("O nome do modificador é obrigatório no snapshot do pedido.");

        if (quantity < 1)
            throw new DomainException("A quantidade do modificador precisa ser pelo menos 1.");

        return new OrderItemModifier
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            OrderItemId = orderItemId,
            ModifierId = modifierId,
            NameSnapshot = nameSnapshot,
            PriceDelta = priceDelta,
            Quantity = quantity
        };
    }
}
