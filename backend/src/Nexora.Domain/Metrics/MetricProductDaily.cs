namespace Nexora.Domain.Metrics;

/// <summary>
/// Agregado de métricas por produto (variante) e dia — base de ranking de vendas e margem
/// por item (RF-BI). Chave composta: <c>(tenantId, storeId, variantId, businessDay)</c> —
/// sem coluna <c>id</c> própria.
/// </summary>
public sealed class MetricProductDaily
{
    private MetricProductDaily() { }

    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid VariantId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public int Quantity { get; private set; }
    public decimal FractionQuantity { get; private set; }
    public decimal Revenue { get; private set; }
    public decimal Cost { get; private set; }
    public decimal Margin { get; private set; }
    public int? AvgPrepSeconds { get; private set; }
    public int Cancelled { get; private set; }
    public int Refired { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static MetricProductDaily Create(Guid tenantId, Guid storeId, Guid variantId, DateOnly businessDay)
    {
        return new MetricProductDaily
        {
            TenantId = tenantId,
            StoreId = storeId,
            VariantId = variantId,
            BusinessDay = businessDay,
            ComputedAt = DateTimeOffset.UtcNow
        };
    }
}
