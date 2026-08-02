using Nexora.Domain.Common;

namespace Nexora.Domain.Finance;

/// <summary>
/// Folha de pagamento de um período (<c>YYYY-MM</c>) — agrega os lançamentos individuais de
/// <see cref="PayrollItem"/>.
/// </summary>
public sealed class Payroll
{
    private Payroll() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? StoreId { get; private set; }
    public string Period { get; private set; } = string.Empty;
    public decimal TotalGross { get; private set; }
    public decimal TotalCharges { get; private set; }
    public decimal TotalNet { get; private set; }
    public string Status { get; private set; } = "DRAFT";
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Payroll Create(Guid tenantId, string period, Guid? storeId = null)
    {
        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("O período da folha de pagamento é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Payroll
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Period = period,
            Status = "DRAFT",
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
