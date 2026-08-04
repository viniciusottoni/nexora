using Microsoft.EntityFrameworkCore;
using Nexora.Domain;

namespace Nexora.Application.Abstractions.Persistence;

/// <summary>
/// Porta de persistência da Application — implementada por <c>AppDbContext</c> em Infrastructure.
/// Só referencia <c>Microsoft.EntityFrameworkCore.Abstractions</c> (DbSet&lt;T&gt;), nunca o
/// provider Npgsql nem migrations (ver nota em Nexora.Application.csproj e ADR-039).
/// Um DbSet por entidade de <c>Nexora.Domain</c>, agrupado por área do domínio.
/// </summary>
public interface IApplicationDbContext
{
    // Platform
    DbSet<Domain.Platform.Tenant> Tenants { get; }
    DbSet<Domain.Platform.TenantConfig> TenantConfigs { get; }
    DbSet<Domain.Platform.Store> Stores { get; }
    DbSet<Domain.Platform.Station> Stations { get; }
    DbSet<Domain.Platform.EdgeInstallation> EdgeInstallations { get; }
    DbSet<Domain.Platform.AppUser> Users { get; }
    DbSet<Domain.Platform.OwnerInvite> OwnerInvites { get; }
    DbSet<Domain.Platform.EmailOutbox> EmailOutboxes { get; }
    DbSet<Domain.Platform.Role> Roles { get; }
    DbSet<Domain.Platform.UserRole> UserRoles { get; }
    DbSet<Domain.Platform.Device> Devices { get; }
    DbSet<Domain.Platform.AuthAttempt> AuthAttempts { get; }
    DbSet<Domain.Platform.AuthSession> AuthSessions { get; }
    DbSet<Domain.Platform.InstallationNonce> InstallationNonces { get; }
    DbSet<Domain.Platform.PairingCode> PairingCodes { get; }
    DbSet<Domain.Platform.AuditLog> AuditLogs { get; }

    /// <summary>
    /// E-09/US-091, filtro <c>minAmount</c> — <c>Before</c>/<c>After</c> são JSONB livre sem coluna
    /// tipada de valor (mesma simplificação já documentada em <see cref="Domain.Platform.AuditLog"/>),
    /// então a extração numérica exige SQL específico do provider (Postgres <c>-&gt;&gt;</c>/cast).
    /// <c>Application</c> só referencia <c>Microsoft.EntityFrameworkCore</c> sem provider (ADR-039)
    /// — por isso este método, implementado em <c>AppDbContext</c> (Infrastructure, que já
    /// referencia Npgsql), existe como porta estreita em vez de a Application montar SQL cru.
    /// </summary>
    IQueryable<Domain.Platform.AuditLog> AuditLogsWithMinAmount(decimal minAmount);
    DbSet<Domain.Platform.IdempotencyKey> IdempotencyKeys { get; }
    DbSet<Domain.Platform.DomainEvent> DomainEvents { get; }
    DbSet<Domain.Platform.MediaAsset> MediaAssets { get; }
    DbSet<Domain.Platform.TenantSecret> TenantSecrets { get; }
    DbSet<Domain.Platform.PushSubscription> PushSubscriptions { get; }

    // Catalog
    DbSet<Domain.Catalog.Category> Categories { get; }
    DbSet<Domain.Catalog.Product> Products { get; }
    DbSet<Domain.Catalog.ProductVariant> ProductVariants { get; }
    DbSet<Domain.Catalog.Price> Prices { get; }
    DbSet<Domain.Catalog.ModifierGroup> ModifierGroups { get; }
    DbSet<Domain.Catalog.Modifier> Modifiers { get; }
    DbSet<Domain.Catalog.ProductModifierGroup> ProductModifierGroups { get; }

    // Operation
    DbSet<Domain.Operation.Area> Areas { get; }
    DbSet<Domain.Operation.DiningTable> DiningTables { get; }
    DbSet<Domain.Operation.TableSession> TableSessions { get; }
    DbSet<Domain.Operation.Order> Orders { get; }
    DbSet<Domain.Operation.OrderItem> OrderItems { get; }
    DbSet<Domain.Operation.OrderItemFraction> OrderItemFractions { get; }
    DbSet<Domain.Operation.OrderItemModifier> OrderItemModifiers { get; }

    // Cashier
    DbSet<Domain.Cashier.CashSession> CashSessions { get; }
    DbSet<Domain.Cashier.CashMovement> CashMovements { get; }
    DbSet<Domain.Cashier.Payment> Payments { get; }
    DbSet<Domain.Cashier.PaymentAllocation> PaymentAllocations { get; }

    // Inventory
    DbSet<Domain.Inventory.UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<Domain.Inventory.Supplier> Suppliers { get; }
    DbSet<Domain.Inventory.Ingredient> Ingredients { get; }
    DbSet<Domain.Inventory.Recipe> Recipes { get; }
    DbSet<Domain.Inventory.RecipeItem> RecipeItems { get; }
    DbSet<Domain.Inventory.StockMovement> StockMovements { get; }
    DbSet<Domain.Inventory.Purchase> Purchases { get; }
    DbSet<Domain.Inventory.PurchaseItem> PurchaseItems { get; }
    DbSet<Domain.Inventory.InventoryCount> InventoryCounts { get; }
    DbSet<Domain.Inventory.InventoryCountItem> InventoryCountItems { get; }

    // Delivery
    DbSet<Domain.Delivery.Customer> Customers { get; }
    DbSet<Domain.Delivery.DeliveryZone> DeliveryZones { get; }
    DbSet<Domain.Delivery.CustomerAddress> CustomerAddresses { get; }
    DbSet<Domain.Delivery.Courier> Couriers { get; }
    DbSet<Domain.Delivery.DeliveryRun> DeliveryRuns { get; }
    DbSet<Domain.Delivery.DeliveryStop> DeliveryStops { get; }

    // Finance
    DbSet<Domain.Finance.FinancialAccount> FinancialAccounts { get; }
    DbSet<Domain.Finance.ExpenseCategory> ExpenseCategories { get; }
    DbSet<Domain.Finance.FinancialEntry> FinancialEntries { get; }
    DbSet<Domain.Finance.Employee> Employees { get; }
    DbSet<Domain.Finance.Payroll> Payrolls { get; }
    DbSet<Domain.Finance.PayrollItem> PayrollItems { get; }

    // Sync
    DbSet<Domain.Sync.Outbox> OutboxEntries { get; }
    DbSet<Domain.Sync.SyncCursor> SyncCursors { get; }
    DbSet<Domain.Sync.SyncConflict> SyncConflicts { get; }

    // Metrics
    DbSet<Domain.Metrics.MetricHourly> MetricHourlies { get; }
    DbSet<Domain.Metrics.MetricDaily> MetricDailies { get; }
    DbSet<Domain.Metrics.MetricProductDaily> MetricProductDailies { get; }
    DbSet<Domain.Metrics.MetricOperatorDaily> MetricOperatorDailies { get; }
    DbSet<Domain.Metrics.Goal> Goals { get; }
    DbSet<Domain.Metrics.Alert> Alerts { get; }

    /// <summary>Único ponto de commit — chamado pelo TransactionBehavior, nunca pelo handler diretamente.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resolve credencial de login por e-mail entre tenants — necessário porque o RLS (ADR-004)
    /// nega leitura de <c>app_user</c> sem <c>app.tenant_id</c> definido, e o login por senha
    /// ainda não sabe a que tenant o e-mail pertence. Porta de <c>auth_lookup_user()</c> +
    /// <c>findByEmail</c> (apps/api-cloud/src/modules/auth/prisma-password-auth.repository.ts).
    /// A função SQL <c>auth_lookup_user(citext)</c> (<c>SECURITY DEFINER</c>) é criada pela
    /// migration <c>CreateAuthLookupUserFunction</c> — ver implementação em
    /// <c>Nexora.Infrastructure.Persistence.AppDbContext.Auth</c>.
    /// </summary>
    Task<LoginCredentialLookup?> FindLoginCredentialByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Define explicitamente <c>app.tenant_id</c> (<c>SET LOCAL</c>) na transação corrente — porta
    /// de <c>setTenant(tx, tenantId)</c> (prisma-password-auth.repository.ts). Uso restrito aos
    /// handlers do módulo Auth, no trecho em que o tenant já é conhecido (decodificado do refresh
    /// token ou resolvido por <see cref="FindLoginCredentialByEmailAsync"/>) mas
    /// <c>ICurrentTenantContext</c> ainda não está autenticado.
    /// </summary>
    Task SetTenantContextAsync(Guid tenantId, CancellationToken cancellationToken);
}

/// <summary>Linha de retorno de <see cref="IApplicationDbContext.FindLoginCredentialByEmailAsync"/>.</summary>
public sealed class LoginCredentialLookup
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}
