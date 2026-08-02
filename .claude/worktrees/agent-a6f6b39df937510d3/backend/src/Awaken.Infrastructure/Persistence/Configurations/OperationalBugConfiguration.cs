using Awaken.Domain.Entities.Bugs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class OperationalBugConfiguration : IEntityTypeConfiguration<OperationalBug>
{
    public void Configure(EntityTypeBuilder<OperationalBug> builder)
    {
        builder.ToTable("operational_bugs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Component).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Origin).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);
        builder.Property(x => x.RelatedErrorId).HasMaxLength(128);
        builder.HasIndex(x => new { x.Severity, x.Status });
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.CreatedByAdminId);
    }
}
