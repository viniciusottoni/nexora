using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Inventory.UnitOfMeasure> UnitsOfMeasure => Set<Domain.Inventory.UnitOfMeasure>();
    public DbSet<Domain.Inventory.Supplier> Suppliers => Set<Domain.Inventory.Supplier>();
    public DbSet<Domain.Inventory.Ingredient> Ingredients => Set<Domain.Inventory.Ingredient>();
    public DbSet<Domain.Inventory.Recipe> Recipes => Set<Domain.Inventory.Recipe>();
    public DbSet<Domain.Inventory.RecipeItem> RecipeItems => Set<Domain.Inventory.RecipeItem>();
    public DbSet<Domain.Inventory.StockMovement> StockMovements => Set<Domain.Inventory.StockMovement>();
    public DbSet<Domain.Inventory.Purchase> Purchases => Set<Domain.Inventory.Purchase>();
    public DbSet<Domain.Inventory.PurchaseItem> PurchaseItems => Set<Domain.Inventory.PurchaseItem>();
    public DbSet<Domain.Inventory.InventoryCount> InventoryCounts => Set<Domain.Inventory.InventoryCount>();
    public DbSet<Domain.Inventory.InventoryCountItem> InventoryCountItems => Set<Domain.Inventory.InventoryCountItem>();
}
