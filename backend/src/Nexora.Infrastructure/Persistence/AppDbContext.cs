using Nexora.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// DbContext único da solution (edge e cloud compartilham o mesmo schema — ADR-036/038).
/// Os <c>DbSet&lt;T&gt;</c> por área vivem nos arquivos parciais <c>AppDbContext.&lt;Area&gt;.cs</c>
/// neste mesmo diretório (Platform, Catalog, Operation, Cashier, Inventory, Delivery, Finance,
/// Sync, Metrics) — este arquivo só tem o construtor e <see cref="OnModelCreating"/>.
/// RLS é aplicado via <see cref="Interceptors.TenantConnectionInterceptor"/>, registrado no
/// <c>Program.cs</c> de cada Api (ADR-038) — nunca por filtro `.Where(tenantId)` manual.
/// </summary>
public partial class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Uma IEntityTypeConfiguration<T> por entidade, em Persistence/Configurations/<Area>/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
