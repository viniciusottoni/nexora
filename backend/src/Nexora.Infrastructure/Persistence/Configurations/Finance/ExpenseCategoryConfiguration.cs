using Nexora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Finance;

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("expense_category");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.Property(c => c.Name).HasColumnName("name").IsRequired();
        builder.Property(c => c.Group).HasColumnName("group");
        builder.Property(c => c.IsCmv).HasColumnName("is_cmv").HasDefaultValue(false);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(c => new { c.TenantId, c.Name }).IsUnique().HasDatabaseName("uq_expense_category_tenant_name");
    }
}
