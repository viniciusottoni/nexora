using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Sync.Outbox> OutboxEntries => Set<Domain.Sync.Outbox>();
    public DbSet<Domain.Sync.SyncCursor> SyncCursors => Set<Domain.Sync.SyncCursor>();
    public DbSet<Domain.Sync.SyncConflict> SyncConflicts => Set<Domain.Sync.SyncConflict>();
}
