using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("supplier");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.Document).HasColumnName("document").HasMaxLength(18);
        builder.Property(s => s.Contact).HasColumnName("contact").HasColumnType("jsonb"); // TODO: tipar quando o formato for definido
        builder.Property(s => s.LeadTimeDays).HasColumnName("lead_time_days").HasColumnType("smallint").HasDefaultValue(1);
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
    }
}
