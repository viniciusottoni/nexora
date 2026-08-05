using Nexora.Domain.Provisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Provisioning;

/// <summary>Catálogo de plataforma — sem <c>tenant_id</c>/RLS (US-142, ADR-013: dado da Replay, não de um estabelecimento).</summary>
internal sealed class BusinessTemplateConfiguration : IEntityTypeConfiguration<BusinessTemplate>
{
    public void Configure(EntityTypeBuilder<BusinessTemplate> builder)
    {
        builder.ToTable("business_template");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.Version).HasColumnName("version").HasDefaultValue(1);
        builder.Property(t => t.ConfigJson).HasColumnName("config").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(t => t.SeedsJson).HasColumnName("seeds").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(t => t.Code).IsUnique().HasDatabaseName("uq_business_template_code");
    }
}
