using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Catalog.Category> Categories => Set<Domain.Catalog.Category>();
    public DbSet<Domain.Catalog.Product> Products => Set<Domain.Catalog.Product>();
    public DbSet<Domain.Catalog.ProductVariant> ProductVariants => Set<Domain.Catalog.ProductVariant>();
    public DbSet<Domain.Catalog.Price> Prices => Set<Domain.Catalog.Price>();
    public DbSet<Domain.Catalog.ModifierGroup> ModifierGroups => Set<Domain.Catalog.ModifierGroup>();
    public DbSet<Domain.Catalog.Modifier> Modifiers => Set<Domain.Catalog.Modifier>();
    public DbSet<Domain.Catalog.ProductModifierGroup> ProductModifierGroups => Set<Domain.Catalog.ProductModifierGroup>();
}
