using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("category");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Name).HasColumnName("name").IsRequired();
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(c => c.AvailableSchedule).HasColumnName("available_schedule").HasColumnType("jsonb");
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasMany(c => c.Products).WithOne(p => p.Category).HasForeignKey(p => p.CategoryId);

        builder.HasIndex(c => new { c.TenantId, c.SortOrder }).HasDatabaseName("idx_category_tenant_sort");
    }
}
