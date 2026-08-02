using Nexora.Domain.Cashier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Cashier;

internal sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("cash_movement");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.Property(m => m.CashSessionId).HasColumnName("cash_session_id");
        builder.Property(m => m.Type).HasColumnName("type");
        builder.Property(m => m.Amount).HasColumnName("amount").HasColumnType("money_amount");
        builder.Property(m => m.Reason).HasColumnName("reason").IsRequired();
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.AuthorizedBy).HasColumnName("authorized_by");
        builder.Property(m => m.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz");
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(m => new { m.TenantId, m.CashSessionId, m.OccurredAt });
    }
}
