using Nexora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Finance;

internal sealed class FinancialEntryConfiguration : IEntityTypeConfiguration<FinancialEntry>
{
    public void Configure(EntityTypeBuilder<FinancialEntry> builder)
    {
        builder.ToTable("financial_entry");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.StoreId).HasColumnName("store_id");
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.CategoryId).HasColumnName("category_id");
        builder.Property(e => e.Type).HasColumnName("type");
        builder.Property(e => e.Amount).HasColumnName("amount").HasColumnType("money_amount");
        builder.Property(e => e.Description).HasColumnName("description").IsRequired();
        builder.Property(e => e.CompetenceDate).HasColumnName("competence_date").HasColumnType("date");
        builder.Property(e => e.DueDate).HasColumnName("due_date").HasColumnType("date");
        builder.Property(e => e.PaidAt).HasColumnName("paid_at").HasColumnType("timestamptz");
        builder.Property(e => e.ReferenceType).HasColumnName("reference_type").HasMaxLength(32);
        builder.Property(e => e.ReferenceId).HasColumnName("reference_id");
        builder.Property(e => e.IsRecurring).HasColumnName("is_recurring").HasDefaultValue(false);
        builder.Property(e => e.Recurrence).HasColumnName("recurrence").HasColumnType("jsonb");
        builder.Property(e => e.ParentEntryId).HasColumnName("parent_entry_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.TenantId, e.CompetenceDate, e.Type });
    }
}
