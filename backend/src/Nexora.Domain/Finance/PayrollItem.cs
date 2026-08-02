using Nexora.Domain.Common;

namespace Nexora.Domain.Finance;

/// <summary>
/// Lançamento individual de um funcionário dentro de uma <see cref="Payroll"/>.
/// </summary>
public sealed class PayrollItem
{
    private PayrollItem() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PayrollId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal Gross { get; private set; }
    public decimal Charges { get; private set; }
    public decimal Benefits { get; private set; }
    public decimal Deductions { get; private set; }
    public decimal Net { get; private set; }
    public string? Notes { get; private set; }

    public static PayrollItem Create(
        Guid tenantId,
        Guid payrollId,
        Guid employeeId,
        decimal gross = 0m,
        decimal charges = 0m,
        decimal benefits = 0m,
        decimal deductions = 0m,
        decimal net = 0m)
    {
        return new PayrollItem
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            PayrollId = payrollId,
            EmployeeId = employeeId,
            Gross = gross,
            Charges = charges,
            Benefits = benefits,
            Deductions = deductions,
            Net = net
        };
    }
}
