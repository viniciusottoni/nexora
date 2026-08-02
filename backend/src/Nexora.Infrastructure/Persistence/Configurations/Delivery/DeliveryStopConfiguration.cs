using Nexora.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Delivery;

internal sealed class DeliveryStopConfiguration : IEntityTypeConfiguration<DeliveryStop>
{
    public void Configure(EntityTypeBuilder<DeliveryStop> builder)
    {
        builder.ToTable("delivery_stop");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.RunId).HasColumnName("run_id");
        builder.Property(s => s.OrderId).HasColumnName("order_id");
        builder.Property(s => s.AddressId).HasColumnName("address_id");
        builder.Property(s => s.Sequence).HasColumnName("sequence").HasColumnType("smallint").HasDefaultValue((short)1);
        builder.Property(s => s.Status).HasColumnName("status").HasDefaultValue(DeliveryStopStatus.Pending);
        builder.Property(s => s.AssignedAt).HasColumnName("assigned_at").HasColumnType("timestamptz");
        builder.Property(s => s.DeliveredAt).HasColumnName("delivered_at").HasColumnType("timestamptz");
        builder.Property(s => s.Outcome).HasColumnName("outcome");
        builder.Property(s => s.OutcomeReason).HasColumnName("outcome_reason");
        builder.Property(s => s.ReceivedBy).HasColumnName("received_by");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(s => s.OrderId).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.RunId, s.Sequence });
    }
}
