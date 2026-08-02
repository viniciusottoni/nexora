using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Operation.Area> Areas => Set<Domain.Operation.Area>();
    public DbSet<Domain.Operation.DiningTable> DiningTables => Set<Domain.Operation.DiningTable>();
    public DbSet<Domain.Operation.TableSession> TableSessions => Set<Domain.Operation.TableSession>();
    public DbSet<Domain.Operation.Order> Orders => Set<Domain.Operation.Order>();
    public DbSet<Domain.Operation.OrderItem> OrderItems => Set<Domain.Operation.OrderItem>();
    public DbSet<Domain.Operation.OrderItemFraction> OrderItemFractions => Set<Domain.Operation.OrderItemFraction>();
    public DbSet<Domain.Operation.OrderItemModifier> OrderItemModifiers => Set<Domain.Operation.OrderItemModifier>();
}
