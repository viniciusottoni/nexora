using Nexora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Finance;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employee");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.StoreId).HasColumnName("store_id");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.Name).HasColumnName("name").IsRequired();
        builder.Property(e => e.RoleTitle).HasColumnName("role_title");
        builder.Property(e => e.Employment).HasColumnName("employment").HasMaxLength(20);
        builder.Property(e => e.Salary).HasColumnName("salary").HasColumnType("money_amount").HasDefaultValue(0m);
        builder.Property(e => e.HiredAt).HasColumnName("hired_at").HasColumnType("date");
        builder.Property(e => e.TerminatedAt).HasColumnName("terminated_at").HasColumnType("date");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
    }
}
