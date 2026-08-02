using Nexora.Domain.Catalog;

namespace Nexora.Domain.Metrics;

/// <summary>
/// Agregado de métricas operacionais por hora, loja e canal — materializado a partir dos
/// eventos de domínio (ADR-006), nunca calculado em tempo de consulta.
/// Chave composta: <c>(tenantId, storeId, hour, channel)</c> — sem coluna <c>id</c> própria.
/// </summary>
public sealed class MetricHourly
{
    private MetricHourly() { }

    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public DateTimeOffset Hour { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public Channel Channel { get; private set; }
    public int Orders { get; private set; }
    public int OrdersCancelled { get; private set; }
    public int Items { get; private set; }
    public int ItemsRefired { get; private set; }
    public decimal Revenue { get; private set; }
    public int? AvgQueueSeconds { get; private set; }
    public int? AvgPrepSeconds { get; private set; }
    public int? AvgCookSeconds { get; private set; }
    public int? AvgExpediteSeconds { get; private set; }
    public int? AvgTotalSeconds { get; private set; }
    public int? P90TotalSeconds { get; private set; }
    public int? MaxTotalSeconds { get; private set; }
    public int OnTimeCount { get; private set; }
    public int LateCount { get; private set; }
    public int OvenBusySeconds { get; private set; }
    public int OvenIdleWithQueueSeconds { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static MetricHourly Create(Guid tenantId, Guid storeId, DateTimeOffset hour, DateOnly businessDay, Channel channel)
    {
        return new MetricHourly
        {
            TenantId = tenantId,
            StoreId = storeId,
            Hour = hour,
            BusinessDay = businessDay,
            Channel = channel,
            ComputedAt = DateTimeOffset.UtcNow
        };
    }
}
