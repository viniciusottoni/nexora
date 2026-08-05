using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        builder.ToTable("tenant_domain");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(d => d.TenantId).HasColumnName("tenant_id");
        builder.Property(d => d.Domain).HasColumnName("domain").HasMaxLength(253).IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(d => d.VerificationToken).HasColumnName("verification_token").IsRequired();
        builder.Property(d => d.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(d => d.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamptz");
        builder.Property(d => d.CertStatus).HasColumnName("cert_status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(d => d.CertIssuedAt).HasColumnName("cert_issued_at").HasColumnType("timestamptz");
        builder.Property(d => d.CertExpiresAt).HasColumnName("cert_expires_at").HasColumnType("timestamptz");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // RN-015: cada domínio resolve exatamente um tenant — unicidade global, não por tenant.
        builder.HasIndex(d => d.Domain).IsUnique().HasDatabaseName("uq_tenant_domain_domain");
        builder.HasIndex(d => d.TenantId).HasDatabaseName("idx_tenant_domain_tenant");
        builder.HasIndex(d => d.CertExpiresAt).HasDatabaseName("idx_tenant_domain_cert_expires").HasFilter("cert_expires_at IS NOT NULL");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId);
    }
}
