using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Abstractions.Persistence;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — porta dos <c>DbSet</c>s do catálogo de
/// planos e do histórico de mudanças. Partial file NOVO (em vez de estender o corpo principal de
/// <see cref="IApplicationDbContext"/>) para não colidir com outras histórias da E-15 editando o
/// mesmo arquivo em paralelo — mesma convenção do partial <c>AppDbContext.Plans.cs</c> em
/// Infrastructure.
/// </summary>
public partial interface IApplicationDbContext
{
    DbSet<Domain.Platform.PlatformPlan> PlatformPlans { get; }
    DbSet<Domain.Platform.TenantPlanHistory> TenantPlanHistories { get; }
}
