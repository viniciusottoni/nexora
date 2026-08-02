using Awaken.Domain.Entities.Bugs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class OperationalBugEventConfiguration : IEntityTypeConfiguration<OperationalBugEvent>
{
    public void Configure(EntityTypeBuilder<OperationalBugEvent> builder)
    {
        builder.ToTable("operational_bug_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BugId).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.OldValue).HasMaxLength(256);
        builder.Property(x => x.NewValue).HasMaxLength(256);
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.HasIndex(x => x.BugId);
    }
}
