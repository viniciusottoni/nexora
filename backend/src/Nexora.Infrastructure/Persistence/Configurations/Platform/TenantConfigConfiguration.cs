using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class TenantConfigConfiguration : IEntityTypeConfiguration<TenantConfig>
{
    public void Configure(EntityTypeBuilder<TenantConfig> builder)
    {
        builder.ToTable("tenant_config");

        builder.HasKey(c => c.TenantId);
        builder.Property(c => c.TenantId).HasColumnName("tenant_id").ValueGeneratedNever();

        // JSONB livre — ver TODO no Domain sobre value object tipado futuro
        builder.Property(c => c.Branding).HasColumnName("branding").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(c => c.Operation).HasColumnName("operation").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(c => c.Thresholds).HasColumnName("thresholds").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(c => c.Modules).HasColumnName("modules").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(c => c.Fiscal).HasColumnName("fiscal").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(c => c.Printers).HasColumnName("printers").HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
        builder.Property(c => c.Payments).HasColumnName("payments").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(c => c.Maintenance).HasColumnName("maintenance").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();

        builder.Property(c => c.CatalogVersion).HasColumnName("catalog_version").HasDefaultValue(1);
        builder.Property(c => c.ConfigVersion).HasColumnName("config_version").HasDefaultValue(1);
        builder.Property(c => c.BrandingVersion).HasColumnName("branding_version").HasDefaultValue(1);

        builder.Property(c => c.TemplateCode).HasColumnName("template_code").HasMaxLength(32); // US-142
        builder.Property(c => c.TemplateVersion).HasColumnName("template_version");

        // US-154 (Gestão de planos e configuração comercial) — capacidades efetivas reconciliadas
        // a partir do platform_plan corrente do tenant.
        builder.Property(c => c.PlanCapabilitiesJson).HasColumnName("plan_capabilities").HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
        builder.Property(c => c.AppliedPlanVersion).HasColumnName("applied_plan_version");

        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        // relação 1:1 com Tenant configurada em TenantConfiguration (lado "um")
    }
}
