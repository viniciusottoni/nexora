using Awaken.Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class SupportTicketEventConfiguration : IEntityTypeConfiguration<SupportTicketEvent>
{
    public void Configure(EntityTypeBuilder<SupportTicketEvent> builder)
    {
        builder.ToTable("support_ticket_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TicketId).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.OldValue).HasMaxLength(256);
        builder.Property(x => x.NewValue).HasMaxLength(256);
        builder.Property(x => x.NoteContent).HasMaxLength(2000);
        builder.HasIndex(x => x.TicketId);
    }
}
