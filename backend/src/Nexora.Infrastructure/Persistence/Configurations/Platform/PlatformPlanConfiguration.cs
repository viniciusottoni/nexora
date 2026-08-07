using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

/// <summary>US-154 · Gestão de planos e configuração comercial — catálogo de plataforma, sem <c>tenant_id</c>/RLS (mesmo padrão de <c>BusinessTemplateConfiguration</c>, ADR-013: dado da Replay, não de um estabelecimento).</summary>
internal sealed class PlatformPlanConfiguration : IEntityTypeConfiguration<PlatformPlan>
{
    public void Configure(EntityTypeBuilder<PlatformPlan> builder)
    {
        builder.ToTable("platform_plan");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        builder.Property(p => p.Version).HasColumnName("version").HasDefaultValue(1);
        builder.Property(p => p.CapabilitiesJson).HasColumnName("capabilities").HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
        builder.Property(p => p.LimitsJson).HasColumnName("limits").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("uq_platform_plan_code");
    }
}
