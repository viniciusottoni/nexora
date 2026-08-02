using Nexora.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("unit_of_measure");

        // Chave primária é o próprio código — não é UUIDv7 (ADR-016 não se aplica a esta
        // tabela de referência, compartilhada entre tenants).
        builder.HasKey(u => u.Code);
        builder.Property(u => u.Code).HasColumnName("code").HasMaxLength(8).ValueGeneratedNever();

        builder.Property(u => u.Name).HasColumnName("name").IsRequired();
        builder.Property(u => u.BaseCode).HasColumnName("base_code").HasMaxLength(8);
        builder.Property(u => u.Factor).HasColumnName("factor").HasColumnType("numeric(18,9)").HasDefaultValue(1m);
    }
}
