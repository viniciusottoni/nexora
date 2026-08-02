using Nexora.Domain.Common;

namespace Nexora.Domain.Metrics;

/// <summary>
/// Meta de negócio para uma métrica (ex.: CMV, ticket médio) em um período, com comparação
/// configurável (LTE, GTE...) — RF-BI.
/// </summary>
public sealed class Goal
{
    private Goal() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? StoreId { get; private set; }
    public string MetricCode { get; private set; } = string.Empty;
    public decimal TargetValue { get; private set; }
    public string Comparison { get; private set; } = "LTE";
    public string Period { get; private set; } = string.Empty;
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public static Goal Create(Guid tenantId, string metricCode, decimal targetValue, string period, DateOnly validFrom, Guid? storeId = null, string comparison = "LTE")
    {
        if (string.IsNullOrWhiteSpace(metricCode))
            throw new DomainException("O código da métrica da meta é obrigatório.");

        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("O período da meta é obrigatório.");

        return new Goal
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            MetricCode = metricCode,
            TargetValue = targetValue,
            Comparison = comparison,
            Period = period,
            ValidFrom = validFrom,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
