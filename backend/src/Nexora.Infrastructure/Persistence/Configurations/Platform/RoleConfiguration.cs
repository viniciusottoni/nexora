using Nexora.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Platform;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("role");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.Property(r => r.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").IsRequired();
        builder.Property(r => r.Permissions).HasColumnName("permissions").HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
        builder.Property(r => r.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique().HasDatabaseName("uq_role_code");

        builder.HasOne(r => r.Tenant).WithMany(t => t.Roles).HasForeignKey(r => r.TenantId);
    }
}
