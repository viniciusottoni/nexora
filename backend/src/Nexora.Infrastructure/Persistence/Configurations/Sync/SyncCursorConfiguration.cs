using Nexora.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nexora.Infrastructure.Persistence.Configurations.Sync;

internal sealed class SyncCursorConfiguration : IEntityTypeConfiguration<SyncCursor>
{
    public void Configure(EntityTypeBuilder<SyncCursor> builder)
    {
        builder.ToTable("sync_cursor");

        // Chave composta — SyncCursor não tem coluna id própria (documento 13, §3).
        builder.HasKey(c => new { c.InstallationId, c.Direction });

        builder.Property(c => c.InstallationId).HasColumnName("installation_id");
        builder.Property(c => c.Direction).HasColumnName("direction").HasColumnType("varchar(8)");
        builder.Property(c => c.LastSeq).HasColumnName("last_seq").HasDefaultValue(0L);
        builder.Property(c => c.LastSuccessAt).HasColumnName("last_success_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
    }
}
