using Nexora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Finance;

internal sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("financial_account");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.Name).HasColumnName("name").IsRequired();
        builder.Property(a => a.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
        builder.Property(a => a.BankInfo).HasColumnName("bank_info").HasColumnType("jsonb");
        builder.Property(a => a.Balance).HasColumnName("balance").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
    }
}
