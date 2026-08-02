using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Application.Abstractions.Security;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Interceptors;
using Nexora.IntegrationTests.Fakes;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Nexora.IntegrationTests.Fixtures;

/// <summary>
/// Sobe um PostgreSQL real (Testcontainers), aplica as migrations reais (<c>InitialCreate</c> +
/// <c>EnableRowLevelSecurity</c>) e cria um segundo papel de login SEM <c>BYPASSRLS</c> — o
/// próprio papel de deploy (<c>postgres</c>) do container é superusuário e superusuário
/// **sempre** ignora RLS mesmo com <c>FORCE ROW LEVEL SECURITY</c> (comportamento documentado do
/// Postgres). Testar isolamento conectado como superusuário provaria isolamento nenhum — daí a
/// necessidade deste segundo login, espelhando o papel <c>app_user_role</c> real de produção
/// (Docs/Domain/10 §1).
/// </summary>
/// <remarks>
/// A senha do login de teste é gerada em memória e não é secreta de produção (ADR-031 não se
/// aplica aqui) — só existe durante a vida do container descartável.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string AppLoginUser = "app_login_test";
    private const string AppLoginPassword = "N3x0ra!TestOnly";

    private PostgreSqlContainer _container = null!;
    private string _appConnectionString = string.Empty;

    /// <summary>String de conexão usando o papel <c>app_user_role</c> (sujeito a RLS) — a mesma
    /// autoridade de conexão que <c>Api.Edge</c>/<c>Api.Cloud</c> usam em produção.</summary>
    public string AppConnectionString => _appConnectionString;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("nexora_it")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();

        var superuserConnectionString = _container.GetConnectionString();

        await using var migrationContext = CreateDbContext(superuserConnectionString, tenantContext: null);
        await migrationContext.Database.MigrateAsync();

        await using var setupConnection = new NpgsqlConnection(superuserConnectionString);
        await setupConnection.OpenAsync();
        await using (var cmd = setupConnection.CreateCommand())
        {
            // IN ROLE app_user_role: mesmo papel de runtime da aplicação (Docs/Domain/10 §1),
            // criado pela migration EnableRowLevelSecurity — NUNCA BYPASSRLS.
            cmd.CommandText =
                $"CREATE USER {AppLoginUser} WITH PASSWORD '{AppLoginPassword}' IN ROLE app_user_role;";
            await cmd.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(superuserConnectionString)
        {
            Username = AppLoginUser,
            Password = AppLoginPassword,
        };
        _appConnectionString = builder.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Cria um <see cref="AppDbContext"/> ligado ao papel <c>app_user_role</c> (RLS ativo) com o
    /// <see cref="TenantConnectionInterceptor"/> real da aplicação, configurado com
    /// <paramref name="tenantContext"/> — o mesmo caminho de código de <c>Api.Edge</c>/
    /// <c>Api.Cloud</c>, só troca a implementação de <see cref="ICurrentTenantContext"/>.
    /// </summary>
    public AppDbContext CreateAppDbContext(ICurrentTenantContext? tenantContext) =>
        CreateDbContext(_appConnectionString, tenantContext);

    private static AppDbContext CreateDbContext(string connectionString, ICurrentTenantContext? tenantContext)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        if (tenantContext is not null)
        {
            var interceptor = new TenantConnectionInterceptor(tenantContext, NullLogger<TenantConnectionInterceptor>.Instance);
            optionsBuilder.AddInterceptors(interceptor);
        }

        return new AppDbContext(optionsBuilder.Options);
    }
}
