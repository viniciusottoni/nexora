using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Metrics.MetricHourly> MetricHourlies => Set<Domain.Metrics.MetricHourly>();
    public DbSet<Domain.Metrics.MetricDaily> MetricDailies => Set<Domain.Metrics.MetricDaily>();
    public DbSet<Domain.Metrics.MetricProductDaily> MetricProductDailies => Set<Domain.Metrics.MetricProductDaily>();
    public DbSet<Domain.Metrics.MetricOperatorDaily> MetricOperatorDailies => Set<Domain.Metrics.MetricOperatorDaily>();
    public DbSet<Domain.Metrics.Goal> Goals => Set<Domain.Metrics.Goal>();
    public DbSet<Domain.Metrics.Alert> Alerts => Set<Domain.Metrics.Alert>();
}
