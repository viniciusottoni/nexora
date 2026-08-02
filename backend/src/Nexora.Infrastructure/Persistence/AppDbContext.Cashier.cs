using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Cashier.CashSession> CashSessions => Set<Domain.Cashier.CashSession>();
    public DbSet<Domain.Cashier.CashMovement> CashMovements => Set<Domain.Cashier.CashMovement>();
    public DbSet<Domain.Cashier.Payment> Payments => Set<Domain.Cashier.Payment>();
    public DbSet<Domain.Cashier.PaymentAllocation> PaymentAllocations => Set<Domain.Cashier.PaymentAllocation>();
}
