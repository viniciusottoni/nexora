using Nexora.Domain.Cashier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Cashier;

internal sealed class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("cash_session");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever(); // UUIDv7 na origem — ADR-016

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.StoreId).HasColumnName("store_id");
        builder.Property(c => c.OperatorId).HasColumnName("operator_id");
        builder.Property(c => c.DeviceId).HasColumnName("device_id");
        builder.Property(c => c.BusinessDay).HasColumnName("business_day").HasColumnType("date");
        builder.Property(c => c.Status).HasColumnName("status").HasDefaultValue(CashSessionStatus.Open);

        builder.Property(c => c.OpeningAmount).HasColumnName("opening_amount").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(c => c.ExpectedAmount).HasColumnName("expected_amount").HasColumnType("money_amount");
        builder.Property(c => c.CountedAmount).HasColumnName("counted_amount").HasColumnType("money_amount");
        builder.Property(c => c.Divergence).HasColumnName("divergence").HasColumnType("money_amount");

        builder.Property(c => c.OpenedAt).HasColumnName("opened_at").HasColumnType("timestamptz");
        builder.Property(c => c.ClosedAt).HasColumnName("closed_at").HasColumnType("timestamptz");
        builder.Property(c => c.ClosedBy).HasColumnName("closed_by");
        builder.Property(c => c.AuthorizedBy).HasColumnName("authorized_by");
        builder.Property(c => c.Justification).HasColumnName("justification");

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasMany(c => c.Movements).WithOne().HasForeignKey(m => m.CashSessionId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TenantId, c.BusinessDay });
    }
}
