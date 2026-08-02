namespace Nexora.Domain.Catalog;

/// <summary>
/// Associação N:N entre <see cref="Product"/> e <see cref="ModifierGroup"/> — chave primária
/// composta (product_id, group_id), sem PK própria (ADR-039: entidade sem identidade agregada).
/// </summary>
public sealed class ProductModifierGroup
{
    private ProductModifierGroup() { }

    public Guid TenantId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid GroupId { get; private set; }
    public short SortOrder { get; private set; }

    public Product Product { get; private set; } = null!;
    public ModifierGroup Group { get; private set; } = null!;

    public static ProductModifierGroup Create(Guid tenantId, Guid productId, Guid groupId, short sortOrder = 0)
    {
        return new ProductModifierGroup
        {
            TenantId = tenantId,
            ProductId = productId,
            GroupId = groupId,
            SortOrder = sortOrder
        };
    }
}
