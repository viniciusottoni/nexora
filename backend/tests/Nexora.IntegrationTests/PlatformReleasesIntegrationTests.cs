using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Installations.Queries.GetInstallationSyncHealth;
using Nexora.Application.Releases.Commands.PublishRelease;
using Nexora.Application.Releases.Queries.GetReleaseRollout;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-146 "Atualização controlada do parque" contra Postgres real (Testcontainers) — publicação/
/// liberação gradual de <c>Release</c>, progresso de rollout entre vários tenants, e a extensão de
/// <c>GetInstallationSyncHealthQuery</c> que agenda <c>EdgeInstallation.TargetVersion</c>.
/// </summary>
[Collection("Postgres")]
public sealed class PlatformReleasesIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public PlatformReleasesIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Publish_Release_Nova_Cria_Registro()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var version = UniqueVersion();
        var result = await sender.Send(new PublishReleaseCommand(version, 10, "Notas de teste"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Release.Version.Should().Be(version);
        result.Value!.Release.RolloutPercent.Should().Be(10);

        var persisted = await db.Releases.AsNoTracking().SingleAsync(r => r.Version == version);
        persisted.RolloutPercent.Should().Be(10);
    }

    /// <summary>Cenário Gherkin "Liberação gradual" (US-146 §4) — republicar amplia, nunca reduz.</summary>
    [Fact]
    public async Task Republicar_Mesma_Versao_Com_Percentual_Maior_Amplia_O_Rollout()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var version = UniqueVersion();
        await sender.Send(new PublishReleaseCommand(version, 10, null));

        var expanded = await sender.Send(new PublishReleaseCommand(version, 50, null));

        expanded.IsSuccess.Should().BeTrue();
        expanded.Value!.Release.RolloutPercent.Should().Be(50);

        var persisted = await db.Releases.AsNoTracking().SingleAsync(r => r.Version == version);
        persisted.RolloutPercent.Should().Be(50);
    }

    [Fact]
    public async Task Republicar_Mesma_Versao_Com_Percentual_Menor_Falha_Sem_Alterar_Nada()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var version = UniqueVersion();
        await sender.Send(new PublishReleaseCommand(version, 50, null));

        var decreased = await sender.Send(new PublishReleaseCommand(version, 10, null));

        decreased.IsFailure.Should().BeTrue();
        decreased.Code.Should().Be(ApiErrorCodes.ReleaseRolloutCannotDecrease);

        var persisted = await db.Releases.AsNoTracking().SingleAsync(r => r.Version == version);
        persisted.RolloutPercent.Should().Be(50, "republicar com percentual menor não pode ter efeito nenhum");
    }

    [Fact]
    public async Task GetReleaseRollout_Versao_Inexistente_Retorna_ReleaseNotFound()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetReleaseRolloutQuery($"9.9.9-{Guid.NewGuid():N}"));

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.ReleaseNotFound);
    }

    /// <summary>
    /// Cenário Gherkin "Liberação gradual" + progresso (US-146 §7/§10). Usa <c>rolloutPercent=50</c>
    /// (nunca 100 — ver <see cref="FindEligibleInstallationId"/>: um release sempre-elegível
    /// contaminaria a linha de base "sem release publicada" de outros testes desta MESMA suite, que
    /// compartilham o container Postgres) e busca, por força bruta, um <c>installationId</c>
    /// elegível para ESTA release específica — resultado determinístico sem depender de sorte de
    /// bucketing por hash no Guid gerado pela entidade.
    /// </summary>
    [Fact]
    public async Task GetReleaseRollout_Conta_Updated_Failed_Pending_Entre_Varios_Tenants()
    {
        var version = UniqueVersion();
        Release release;

        await using (var publishDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            await using (var publishProvider = BuildContainer(publishDb))
            {
                var publishSender = publishProvider.GetRequiredService<ISender>();
                var published = await publishSender.Send(new PublishReleaseCommand(version, 50, null));
                published.IsSuccess.Should().BeTrue();
            }

            release = await publishDb.Releases.AsNoTracking().SingleAsync(r => r.Version == version);
        }

        // Três instalações em três tenants, cada uma com um Id ACHADO elegível para esta release
        // específica: uma já atualizada, uma que falhou tentando ESTA versão, uma ainda pendente.
        var updated = await SeedInstalledTenantAsync(version, targetVersion: null, lastUpdateStatus: null, installationId: FindEligibleInstallationId(release));
        var failed = await SeedInstalledTenantAsync("1.0.0", targetVersion: version, lastUpdateStatus: nameof(EdgeUpdateStatus.Failed), installationId: FindEligibleInstallationId(release));
        var pending = await SeedInstalledTenantAsync("1.0.0", targetVersion: version, lastUpdateStatus: null, installationId: FindEligibleInstallationId(release));

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetReleaseRolloutQuery(version));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().BeGreaterThanOrEqualTo(3);
        result.Value!.Updated.Should().BeGreaterThanOrEqualTo(1);
        result.Value!.Failed.Should().BeGreaterThanOrEqualTo(1);
        result.Value!.Pending.Should().BeGreaterThanOrEqualTo(1);
        (result.Value!.Updated + result.Value!.Failed + result.Value!.Pending).Should().Be(result.Value!.Total);

        _ = (updated, failed, pending);
    }

    /// <summary>Extensão de US-006: instalação elegível para uma release publicada recebe TargetVersion.</summary>
    [Fact]
    public async Task GetInstallationSyncHealth_Instalacao_Elegivel_Recebe_TargetVersion()
    {
        var version = UniqueVersion();
        Release release;

        await using (var publishDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            await using (var publishProvider = BuildContainer(publishDb))
            {
                var publishSender = publishProvider.GetRequiredService<ISender>();
                // rolloutPercent=50 (nunca 100 — ver comentário de GetReleaseRollout_Conta_...).
                await publishSender.Send(new PublishReleaseCommand(version, 50, null));
            }

            release = await publishDb.Releases.AsNoTracking().SingleAsync(r => r.Version == version);
        }

        var eligibleInstallationId = FindEligibleInstallationId(release);
        var (tenantId, installationId) = await SeedTenantWithConfigAsync("1.0.0", eligibleInstallationId);

        var tenantContext = new StaticTenantContext(tenantId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetInstallationSyncHealthQuery(tenantId, installationId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpectedVersion.Should().Be(version);

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(i => i.Id == installationId);
        persisted.TargetVersion.Should().Be(version);
    }

    /// <summary>
    /// Baseline pré-US-146: para uma instalação que não é elegível para NENHUMA release
    /// atualmente publicada (buscada por força bruta contra o que já existe nesta suite — nenhum
    /// teste aqui publica <c>rolloutPercent=100</c>, então essa busca sempre termina), cai no
    /// fallback de <c>IAppVersionProvider.CurrentVersion</c>.
    /// </summary>
    [Fact]
    public async Task GetInstallationSyncHealth_Sem_Release_Elegivel_Usa_Fallback_Da_Versao_Corrente()
    {
        Guid notEligibleInstallationId;
        await using (var scanDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            notEligibleInstallationId = await FindInstallationIdNotEligibleForAnyReleaseAsync(scanDb);
        }

        var (tenantId, installationId) = await SeedTenantWithConfigAsync("1.0.0", notEligibleInstallationId);

        var tenantContext = new StaticTenantContext(tenantId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetInstallationSyncHealthQuery(tenantId, installationId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpectedVersion.Should().Be(FixedVersion);

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(i => i.Id == installationId);
        persisted.TargetVersion.Should().BeNull("sem release elegível, nada muda no comportamento pré-existente");
    }

    private const string FixedVersion = "1.4.2";

    // Version tem HasMaxLength(20) (ReleaseConfiguration) — formato compacto para caber com folga
    // e ainda ser praticamente único por execução de teste.
    private static string UniqueVersion() => $"9.{Random.Shared.Next(100_000, 999_999)}.{Random.Shared.Next(1000, 9999)}";

    /// <summary>Busca por força bruta um Guid elegível para <paramref name="release"/> — barato (rolloutPercent=50 acerta em ~2 tentativas em média).</summary>
    private static Guid FindEligibleInstallationId(Release release)
    {
        for (var i = 0; i < 10_000; i++)
        {
            var candidate = Guid.NewGuid();
            if (release.IsEligibleFor(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Não encontrou installationId elegível para a release — ajuste o rolloutPercent do teste.");
    }

    /// <summary>
    /// Busca por força bruta um Guid que NÃO é elegível para NENHUMA release com
    /// <c>RolloutPercent &gt; 0</c> hoje publicada nesta suite — só termina porque nenhum teste
    /// aqui usa <c>rolloutPercent=100</c> (ver docstring da suite).
    /// </summary>
    private static async Task<Guid> FindInstallationIdNotEligibleForAnyReleaseAsync(IApplicationDbContext db)
    {
        var releases = await db.Releases.AsNoTracking().Where(r => r.RolloutPercent > 0).ToListAsync();

        for (var i = 0; i < 10_000; i++)
        {
            var candidate = Guid.NewGuid();
            if (!releases.Any(r => r.IsEligibleFor(candidate)))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Não encontrou installationId não-elegível — releases demais publicadas nesta suite.");
    }

    private async Task<(Guid TenantId, Guid InstallationId)> SeedTenantWithConfigAsync(string installedVersion, Guid? installationId = null)
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        Guid resolvedInstallationId;
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            db.TenantConfigs.Add(TenantConfig.Create(tenantId));

            var store = Store.Create(tenantId, "Matriz", isDefault: true);
            db.Stores.Add(store);

            var installation = EdgeInstallation.CreateInstalled(
                installationId ?? Guid.NewGuid(), tenantId, store.Id, "Servidor local — Matriz", publicKey: "pk-teste", version: installedVersion);
            db.EdgeInstallations.Add(installation);

            await db.SaveChangesAsync();
            resolvedInstallationId = installation.Id;
        }

        return (tenantId, resolvedInstallationId);
    }

    private async Task<(Guid TenantId, Guid InstallationId)> SeedInstalledTenantAsync(
        string installedVersion, string? targetVersion, string? lastUpdateStatus, Guid? installationId = null)
    {
        var (tenantId, resolvedInstallationId) = await SeedTenantWithConfigAsync(installedVersion, installationId);

        if (targetVersion is not null || lastUpdateStatus is not null)
        {
            await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
            var appDb = (Nexora.Infrastructure.Persistence.AppDbContext)db;

            if (targetVersion is not null)
            {
                await appDb.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE edge_installation SET target_version = {targetVersion} WHERE id = {resolvedInstallationId}");
            }

            if (lastUpdateStatus is not null)
            {
                await appDb.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE edge_installation SET last_update_status = {lastUpdateStatus}, last_update_at = now() WHERE id = {resolvedInstallationId}");
            }
        }

        return (tenantId, resolvedInstallationId);
    }

    private static ServiceProvider BuildContainer(IApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton<ICurrentTenantContext>(new StaticTenantContext(tenantId: null));
        services.AddSingleton<IEventOriginProvider, CloudEventOriginProvider>();
        services.AddSingleton<IAppVersionProvider>(new FixedAppVersionProvider(FixedVersion));
        services.AddSingleton<IPlatformAlertNotifier>(new RecordingPlatformAlertNotifier());

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ICommand).Assembly);

        return services.BuildServiceProvider();
    }

    private sealed class FixedAppVersionProvider : IAppVersionProvider
    {
        public FixedAppVersionProvider(string version) => CurrentVersion = version;
        public string CurrentVersion { get; }
    }
}
