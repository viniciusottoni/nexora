using Nexora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Finance;

internal sealed class PayrollConfiguration : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> builder)
    {
        builder.ToTable("payroll");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.StoreId).HasColumnName("store_id");
        builder.Property(p => p.Period).HasColumnName("period").HasColumnType("char(7)").IsRequired();
        builder.Property(p => p.TotalGross).HasColumnName("total_gross").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(p => p.TotalCharges).HasColumnName("total_charges").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(p => p.TotalNet).HasColumnName("total_net").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(p => p.Status).HasColumnName("status").HasMaxLength(16).HasDefaultValue("DRAFT");
        builder.Property(p => p.ApprovedBy).HasColumnName("approved_by");
        builder.Property(p => p.PaidAt).HasColumnName("paid_at").HasColumnType("timestamptz");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(p => new { p.TenantId, p.StoreId, p.Period }).IsUnique().HasDatabaseName("uq_payroll_tenant_store_period");
    }
}
