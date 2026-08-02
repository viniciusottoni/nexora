using Awaken.Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class SecurityAlertConfiguration : IEntityTypeConfiguration<SecurityAlert>
{
    public void Configure(EntityTypeBuilder<SecurityAlert> builder)
    {
        builder.ToTable("security_alerts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AlertType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Origin).HasMaxLength(256);
        builder.Property(x => x.MaskedIp).HasMaxLength(64);
        builder.Property(x => x.Environment).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Classification).HasMaxLength(32);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => new { x.AlertType, x.Status });
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.Classification);
    }
}
