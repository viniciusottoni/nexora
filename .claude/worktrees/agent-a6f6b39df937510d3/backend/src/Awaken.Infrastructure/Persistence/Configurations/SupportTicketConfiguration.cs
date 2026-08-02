using Awaken.Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AppVersion).HasMaxLength(32);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);
        builder.Property(x => x.Priority).HasMaxLength(16);
        builder.Property(x => x.AssignedAdminId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}
