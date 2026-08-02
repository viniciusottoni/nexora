using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// Fábrica usada só por <c>dotnet ef migrations add</c>/<c>dotnet ef database update</c>
/// (ferramenta de design-time — <c>Microsoft.EntityFrameworkCore.Design</c> é <c>PrivateAssets="all"</c>
/// no <c>.csproj</c>, não entra no runtime publicado). Evita depender de <c>Nexora.Api.Cloud</c>/
/// <c>Nexora.Api.Edge</c> como "startup project" (nenhum dos dois é obrigatório: basta
/// <c>--project src/Nexora.Infrastructure</c>).
/// </summary>
/// <remarks>
/// A connection string aqui **nunca** é usada em runtime — só para o EF Core conseguir abrir uma
/// conexão momentânea o suficiente para inspecionar o catálogo do Postgres ao gerar/aplicar
/// migrations. Runtime real usa <c>appsettings.json</c>/<c>ConnectionStrings:DefaultConnection</c>
/// via <c>Program.cs</c> de cada Api (ADR-038).
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("NEXORA_MIGRATIONS_CONNECTION") ??
            "Host=localhost;Port=5432;Database=nexora_design;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}
