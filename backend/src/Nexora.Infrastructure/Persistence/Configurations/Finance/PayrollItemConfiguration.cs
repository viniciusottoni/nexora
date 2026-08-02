using Nexora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Finance;

internal sealed class PayrollItemConfiguration : IEntityTypeConfiguration<PayrollItem>
{
    public void Configure(EntityTypeBuilder<PayrollItem> builder)
    {
        builder.ToTable("payroll_item");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.Property(i => i.PayrollId).HasColumnName("payroll_id");
        builder.Property(i => i.EmployeeId).HasColumnName("employee_id");
        builder.Property(i => i.Gross).HasColumnName("gross").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(i => i.Charges).HasColumnName("charges").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(i => i.Benefits).HasColumnName("benefits").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(i => i.Deductions).HasColumnName("deductions").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(i => i.Net).HasColumnName("net").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(i => i.Notes).HasColumnName("notes");

        builder.HasIndex(i => new { i.PayrollId, i.EmployeeId }).IsUnique().HasDatabaseName("uq_payroll_item_payroll_employee");
    }
}
