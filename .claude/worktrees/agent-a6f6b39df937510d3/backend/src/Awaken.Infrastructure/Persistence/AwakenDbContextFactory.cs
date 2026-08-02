using Awaken.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Awaken.Infrastructure.Persistence;

/// <summary>
/// Fábrica de design-time para o `dotnet ef migrations`/`dotnet ef database update`.
///
/// Por que existe: o host completo (Program.cs + DependencyInjection.AddInfrastructure)
/// registra módulos de outras US do EPIC-017 ainda em desenvolvimento em paralelo nesta
/// branch (ex.: diagnósticos de mídia/assinatura), o que faz a validação de DI do
/// ASP.NET Core falhar em tempo de design mesmo quando o build normal é bem-sucedido.
/// Esta fábrica monta um DbContext mínimo e isolado, sem depender do host inteiro,
/// para que a ferramenta de migrations funcione independente do estado dos demais módulos.
/// Não afeta o runtime da aplicação (Program.cs/DependencyInjection.cs não são usados aqui).
/// </summary>
public class AwakenDbContextFactory : IDesignTimeDbContextFactory<AwakenDbContext>
{
    public AwakenDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Awaken.Api");
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(basePath) ? basePath : Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = configBuilder.Build();

        var connectionString = configuration.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = "Host=localhost;Port=5432;Database=awaken;Username=awaken;Password=awaken_dev_password";
        }

        var optionsBuilder = new DbContextOptionsBuilder<AwakenDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            b => b.MigrationsAssembly(typeof(AwakenDbContext).Assembly.FullName));

        return new AwakenDbContext(optionsBuilder.Options, new DateTimeService());
    }
}
