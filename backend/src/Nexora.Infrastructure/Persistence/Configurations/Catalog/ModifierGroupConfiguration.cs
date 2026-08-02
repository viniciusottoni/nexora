using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ModifierGroupConfiguration : IEntityTypeConfiguration<ModifierGroup>
{
    public void Configure(EntityTypeBuilder<ModifierGroup> builder)
    {
        builder.ToTable("modifier_group");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(g => g.TenantId).HasColumnName("tenant_id");
        builder.Property(g => g.Name).HasColumnName("name").IsRequired();
        builder.Property(g => g.MinSelect).HasColumnName("min_select").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(g => g.MaxSelect).HasColumnName("max_select").HasColumnType("smallint").HasDefaultValue((short)1);
        builder.Property(g => g.IsRequired).HasColumnName("is_required").HasDefaultValue(false);
        builder.Property(g => g.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(g => g.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(g => g.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasMany(g => g.Modifiers).WithOne(m => m.Group).HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
        // relação ModifierGroup <-> ProductModifierGroup (join N:N) configurada em ProductModifierGroupConfiguration
    }
}
