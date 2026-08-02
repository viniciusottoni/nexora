using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Delivery.Customer> Customers => Set<Domain.Delivery.Customer>();
    public DbSet<Domain.Delivery.DeliveryZone> DeliveryZones => Set<Domain.Delivery.DeliveryZone>();
    public DbSet<Domain.Delivery.CustomerAddress> CustomerAddresses => Set<Domain.Delivery.CustomerAddress>();
    public DbSet<Domain.Delivery.Courier> Couriers => Set<Domain.Delivery.Courier>();
    public DbSet<Domain.Delivery.DeliveryRun> DeliveryRuns => Set<Domain.Delivery.DeliveryRun>();
    public DbSet<Domain.Delivery.DeliveryStop> DeliveryStops => Set<Domain.Delivery.DeliveryStop>();
}
