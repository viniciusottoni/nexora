using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — <c>DbSet</c>s do catálogo de planos e do
/// histórico de mudanças. Partial file NOVO (em vez de estender <c>AppDbContext.Platform.cs</c>)
/// para não colidir com outras histórias da E-15 editando o mesmo arquivo em paralelo.
/// </summary>
public partial class AppDbContext
{
    public DbSet<Domain.Platform.PlatformPlan> PlatformPlans => Set<Domain.Platform.PlatformPlan>();
    public DbSet<Domain.Platform.TenantPlanHistory> TenantPlanHistories => Set<Domain.Platform.TenantPlanHistory>();
}
