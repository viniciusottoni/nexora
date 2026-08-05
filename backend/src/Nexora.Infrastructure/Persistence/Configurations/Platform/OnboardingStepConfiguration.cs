using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class OnboardingStepConfiguration : IEntityTypeConfiguration<OnboardingStep>
{
    public void Configure(EntityTypeBuilder<OnboardingStep> builder)
    {
        builder.ToTable("onboarding_step");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.Key).HasColumnName("key").HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(s => s.CompletedBy).HasColumnName("completed_by");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.TenantId, s.Key }).IsUnique().HasDatabaseName("uq_onboarding_step_tenant_key");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId);
    }
}
