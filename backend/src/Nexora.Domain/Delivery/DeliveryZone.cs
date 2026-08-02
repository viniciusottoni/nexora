using Nexora.Domain.Common;

namespace Nexora.Domain.Delivery;

/// <summary>
/// Área de entrega de uma loja — define taxa, pedido mínimo e tempo médio por região (RF-DLV).
/// </summary>
public sealed class DeliveryZone
{
    private DeliveryZone() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // TODO: tipar quando o formato da geometria (polígono) for definido
    public string? Geometry { get; private set; }

    public IReadOnlyList<string> Districts { get; private set; } = Array.Empty<string>();
    public decimal Fee { get; private set; }
    public decimal MinOrder { get; private set; }
    public int AvgMinutes { get; private set; } = 20;
    public decimal? MaxDistanceKm { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public static DeliveryZone Create(Guid tenantId, Guid storeId, string name, decimal fee = 0m, decimal minOrder = 0m, int avgMinutes = 20)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da área de entrega é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new DeliveryZone
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Name = name,
            Districts = Array.Empty<string>(),
            Fee = fee,
            MinOrder = minOrder,
            AvgMinutes = avgMinutes,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
