using Nexora.Domain.Catalog;

namespace Nexora.Domain.Metrics;

/// <summary>
/// Agregado de métricas operacionais e financeiras por dia, loja e canal — base dos relatórios
/// e comparativos (ADR-018, "mesmo dia da semana do mês passado").
/// Chave composta: <c>(tenantId, storeId, businessDay, channel)</c> — sem coluna <c>id</c> própria.
/// </summary>
public sealed class MetricDaily
{
    private MetricDaily() { }

    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public Channel Channel { get; private set; }
    public int Orders { get; private set; }
    public int OrdersCancelled { get; private set; }
    public int Items { get; private set; }
    public decimal Revenue { get; private set; }
    public decimal Discounts { get; private set; }
    public decimal ServiceFee { get; private set; }
    public decimal AvgTicket { get; private set; }
    public int Covers { get; private set; }
    public int Sessions { get; private set; }
    public decimal? TableTurns { get; private set; }
    public int? AvgStaySeconds { get; private set; }
    public int? AvgTotalSeconds { get; private set; }
    public int? P90TotalSeconds { get; private set; }
    public decimal? OnTimeRate { get; private set; }
    public decimal CmvTheoretical { get; private set; }
    public decimal LaborCost { get; private set; }
    public decimal CardFees { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static MetricDaily Create(Guid tenantId, Guid storeId, DateOnly businessDay, Channel channel)
    {
        return new MetricDaily
        {
            TenantId = tenantId,
            StoreId = storeId,
            BusinessDay = businessDay,
            Channel = channel,
            ComputedAt = DateTimeOffset.UtcNow
        };
    }
}
