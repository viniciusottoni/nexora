using Nexora.Domain.Common;

namespace Nexora.Domain.Delivery;

/// <summary>
/// Rota de entrega de um entregador em um dia operacional — agrupa uma ou mais paradas
/// (<see cref="DeliveryStop"/>).
/// </summary>
public sealed class DeliveryRun
{
    private DeliveryRun() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid CourierId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public DateTimeOffset? ArrivedAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }
    public int StopsCount { get; private set; }
    public decimal? DistanceKm { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static DeliveryRun Create(Guid tenantId, Guid storeId, Guid courierId, DateOnly businessDay)
    {
        return new DeliveryRun
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            CourierId = courierId,
            BusinessDay = businessDay,
            StopsCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
