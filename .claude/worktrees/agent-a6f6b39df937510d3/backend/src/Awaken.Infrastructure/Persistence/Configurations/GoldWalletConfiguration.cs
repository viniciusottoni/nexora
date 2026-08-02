using Awaken.Domain.Entities.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class GoldWalletConfiguration : IEntityTypeConfiguration<GoldWallet>
{
    public void Configure(EntityTypeBuilder<GoldWallet> builder)
    {
        builder.ToTable("gold_wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId).IsRequired();
        builder.Property(w => w.Balance).IsRequired();

        // RN-005: concorrencia otimista via coluna de sistema "xmin" do
        // PostgreSQL (MVCC), evitando que movimentacoes concorrentes
        // corrompam o saldo sem precisar de uma coluna de versao propria.
        // Mapeia a coluna de sistema existente - nao e criada pela migration.
        builder.Property(w => w.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasIndex(w => w.UserId).IsUnique();
    }
}
