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
        builder.Property(t => t.Status).HasColumnName("status").HasDefaultValue(TenantStatus.Provisioned);

        // US-153 (Ciclo de vida do estabelecimento) — controle de concorrência otimista (header
        // If-Match) da máquina de estados canônica.
        builder.Property(t => t.StatusVersion).HasColumnName("status_version").HasDefaultValue(1);
        builder.Property(t => t.Plan).HasColumnName("plan").HasMaxLength(32).HasDefaultValue("STANDARD");

        // US-154 (Gestão de planos e configuração comercial) — concorrência otimista do plano
        // comercial, dimensão independente de StatusVersion (ver docstring de Tenant.PlanVersion).
        builder.Property(t => t.PlanVersion).HasColumnName("plan_version").HasDefaultValue(1);
        builder.Property(t => t.Timezone).HasColumnName("timezone").HasDefaultValue("America/Sao_Paulo");
        builder.Property(t => t.Locale).HasColumnName("locale").HasDefaultValue("pt-BR");
        builder.Property(t => t.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("BRL");
        builder.Property(t => t.Domain).HasColumnName("domain");

        // US-151 (Diretório de estabelecimentos) — denormalizado aqui pelo mesmo motivo de Plan:
        // ver docstring de Tenant.OwnerEmail/Tenant.TemplateCode.
        builder.Property(t => t.OwnerEmail).HasColumnName("owner_email").HasMaxLength(255);
        builder.Property(t => t.TemplateCode).HasColumnName("template_code").HasMaxLength(32);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("uq_tenant_slug");
        builder.HasIndex(t => t.Domain).IsUnique().HasDatabaseName("uq_tenant_domain");

        // US-151 §14 DoD "índices e plano de execução verificados" — suportam os filtros/ordenação
        // do diretório (status, criação, modelo). idx_tenant_name/idx_tenant_owner_email (expressão
        // lower(...), para ILIKE case-insensitive) não têm equivalente na Fluent API do EF Core
        // (sem suporte nativo a índice de expressão) — criados via SQL cru na migration
        // AddTenantDirectorySearchFields, não aqui.
        builder.HasIndex(t => t.CreatedAt).HasDatabaseName("idx_tenant_created_at");
        builder.HasIndex(t => t.Status).HasDatabaseName("idx_tenant_status");
        builder.HasIndex(t => t.TemplateCode).HasDatabaseName("idx_tenant_template_code");

        // relações sem navegação de volta ficam do lado "um"; as demais são configuradas
        // a partir da entidade filha (AppUser, Role, Device, EdgeInstallation, AuditLog).
        builder.HasOne(t => t.Config).WithOne(c => c.Tenant)
            .HasForeignKey<TenantConfig>(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Stores).WithOne().HasForeignKey(s => s.TenantId);
    }
}
