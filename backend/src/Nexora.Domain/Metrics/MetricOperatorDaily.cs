using Nexora.Domain.Common;

namespace Nexora.Domain.Metrics;

/// <summary>
/// Agregado de métricas por operador (usuário), papel e dia — base de ranking de desempenho
/// individual (RF-BI). Chave composta: <c>(tenantId, storeId, userId, businessDay, roleContext)</c>
/// — sem coluna <c>id</c> própria.
/// </summary>
public sealed class MetricOperatorDaily
{
    private MetricOperatorDaily() { }

    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public string RoleContext { get; private set; } = string.Empty;
    public int Orders { get; private set; }
    public int Items { get; private set; }
    public decimal Revenue { get; private set; }
    public decimal AvgTicket { get; private set; }
    public int Sessions { get; private set; }
    public int? AvgServeSeconds { get; private set; }
    public int UpsellOffered { get; private set; }
    public int UpsellAccepted { get; private set; }
    public int Cancellations { get; private set; }
    public decimal DiscountsGiven { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static MetricOperatorDaily Create(Guid tenantId, Guid storeId, Guid userId, DateOnly businessDay, string roleContext)
    {
        if (string.IsNullOrWhiteSpace(roleContext))
            throw new DomainException("O contexto de papel do operador é obrigatório.");

        return new MetricOperatorDaily
        {
            TenantId = tenantId,
            StoreId = storeId,
            UserId = userId,
            BusinessDay = businessDay,
            RoleContext = roleContext,
            ComputedAt = DateTimeOffset.UtcNow
        };
    }
}
