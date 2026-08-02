using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenant"); // raiz da hierarquia — sem tenant_id, sem RLS (ADR-004)

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever(); // UUIDv7 na origem — ADR-016

        builder.Property(t => t.Slug).HasColumnName("slug").HasColumnType("slug").HasMaxLength(64).IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.LegalName).HasColumnName("legal_name");
        builder.Property(t => t.Document).HasColumnName("document").HasMaxLength(18);
        builder.Property(t => t.Status).HasColumnName("status").HasDefaultValue(TenantStatus.Trial);
        builder.Property(t => t.Plan).HasColumnName("plan").HasMaxLength(32).HasDefaultValue("STANDARD");
        builder.Property(t => t.Timezone).HasColumnName("timezone").HasDefaultValue("America/Sao_Paulo");
        builder.Property(t => t.Locale).HasColumnName("locale").HasDefaultValue("pt-BR");
        builder.Property(t => t.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("BRL");
        builder.Property(t => t.Domain).HasColumnName("domain");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("uq_tenant_slug");
        builder.HasIndex(t => t.Domain).IsUnique().HasDatabaseName("uq_tenant_domain");

        // relações sem navegação de volta ficam do lado "um"; as demais são configuradas
        // a partir da entidade filha (AppUser, Role, Device, EdgeInstallation, AuditLog).
        builder.HasOne(t => t.Config).WithOne(c => c.Tenant)
            .HasForeignKey<TenantConfig>(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Stores).WithOne().HasForeignKey(s => s.TenantId);
    }
}
