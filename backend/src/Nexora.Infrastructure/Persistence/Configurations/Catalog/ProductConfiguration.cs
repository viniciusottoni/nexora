using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Catalog;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.Property(p => p.CategoryId).HasColumnName("category_id");
        builder.Property(p => p.StationId).HasColumnName("station_id");
        builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        builder.Property(p => p.Description).HasColumnName("description");
        builder.Property(p => p.IngredientsText).HasColumnName("ingredients_text");

        // String[] do Prisma vira text[] nativo do Postgres (Npgsql) — comparador de valor
        // explícito porque IReadOnlyList<string> não tem igualdade estrutural por padrão.
        builder.Property(p => p.Allergens)
            .HasColumnName("allergens")
            .HasColumnType("text[]")
            .HasConversion(
                allergens => allergens.ToArray(),
                array => (IReadOnlyList<string>)array.ToList())
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                (a, b) => a!.SequenceEqual(b!),
                a => a.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                a => (IReadOnlyList<string>)a.ToList()));

        builder.Property(p => p.SortOrder).HasColumnName("sort_order").HasColumnType("smallint").HasDefaultValue((short)0);
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);
        builder.Property(p => p.UnavailableReason).HasColumnName("unavailable_reason");
        builder.Property(p => p.UnavailableSince).HasColumnName("unavailable_since").HasColumnType("timestamptz");
        builder.Property(p => p.AllowsFractions).HasColumnName("allows_fractions").HasDefaultValue(false);
        builder.Property(p => p.MaxFractions).HasColumnName("max_fractions").HasColumnType("smallint").HasDefaultValue((short)1);
        builder.Property(p => p.FractionGroup).HasColumnName("fraction_group");
        builder.Property(p => p.Ncm).HasColumnName("ncm").HasMaxLength(10);
        builder.Property(p => p.Cest).HasColumnName("cest").HasMaxLength(10);
        builder.Property(p => p.Cfop).HasColumnName("cfop").HasMaxLength(5);
        builder.Property(p => p.OriginCode).HasColumnName("origin_code").HasColumnType("smallint");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // relação Category -> Products configurada uma única vez em CategoryConfiguration (lado "um")
        builder.HasMany(p => p.Variants).WithOne(v => v.Product).HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
        // relação Product <-> ProductModifierGroup (join N:N) configurada em ProductModifierGroupConfiguration

        builder.HasIndex(p => new { p.TenantId, p.CategoryId, p.SortOrder }).HasDatabaseName("idx_product_tenant_category_sort");
    }
}
