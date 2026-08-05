using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Installations.Commands.RunEdgeUpdateCycle;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Persistence;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-146 §7 "RunEdgeUpdateCycleCommand" contra Postgres real (Testcontainers) — o ciclo completo
/// do lado edge (janela → pendências de sincronização → backup → download → migration → health
/// check → ativa ou reverte). <see cref="FakeEdgeUpdateExecutor"/> controla deterministicamente
/// cada passo, sem depender de infraestrutura de container/Docker real (fora do alcance deste
/// sandbox — ver docstring de <c>SimulatedEdgeUpdateExecutor</c>, a implementação de produção).
/// </summary>
[Collection("Postgres")]
public sealed class EdgeUpdateCycleIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public EdgeUpdateCycleIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Sem_TargetVersion_Nao_Faz_Nada()
    {
        var (tenantId, installationId) = await SeedInstallationAsync(CurrentHourWindowJson());

        var executor = new FakeEdgeUpdateExecutor();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = BuildContainer(db, executor);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RunEdgeUpdateCycleCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("NoUpdatePending");

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(i => i.Id == installationId);
        persisted.LastUpdateStatus.Should().BeNull();
    }

    [Fact]
    public async Task Fora_Da_Janela_Configurada_Nao_Tenta_Atualizar()
    {
        var (tenantId, installationId) = await SeedInstallationAsync(OutsideCurrentHourWindowJson());
        await SetTargetVersionAsync(tenantId, installationId, "1.5.0");

        var executor = new FakeEdgeUpdateExecutor();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = BuildContainer(db, executor);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RunEdgeUpdateCycleCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("OutsideWindow");

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(i => i.Id == installationId);
        persisted.LastUpdateStatus.Should().BeNull("fora da janela, o ciclo não deve sequer tentar backup/download/migration");
        executor.RollbackCallCount.Should().Be(0);
    }

    /// <summary>Cenário Gherkin "Instalação com pendência de sincronização" (US-146 §4).</summary>
    [Fact]
    public async Task Com_Eventos_Pendentes_Acima_Do_Limiar_Adia_E_Informa_A_Plataforma()
    {
        var (tenantId, installationId) = await SeedInstallationAsync(CurrentHourWindowJson());
        await SetTargetVersionAsync(tenantId, installationId, "1.5.0");
        await SeedPendingOutboxAsync(tenantId, count: Nexora.Application.Installations.Support.EdgeUpdateSyncPendingPolicy.PendingEventsThreshold);

        var executor = new FakeEdgeUpdateExecutor();
        var notifier = new RecordingPlatformAlertNotifier();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = BuildContainer(db, executor, notifier);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RunEdgeUpdateCycleCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(EdgeUpdateStatus.Deferred));

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(i => i.Id == installationId);
        persisted.LastUpdateStatus.Should().Be(nameof(EdgeUpdateStatus.Deferred));
        persisted.TargetVersion.Should().Be("1.5.0", "continua pendente para a próxima janela");

        notifier.EdgeUpdateDeferredCalls.Should().ContainSingle();
        notifier.EdgeUpdateDeferredCalls.Single().InstallationId.Should().Be(installationId);

        executor.RollbackCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Ciclo_Completo_Com_Sucesso_Ativa_Nova_Versao()
    {
        var (tenantId, installationId) = await SeedInstallationAsync(CurrentHourWindowJson());
        await SetTargetVersionAsync(tenantId, installationId, "1.5.0");

        var executor = new FakeEdgeUpdateExecutor();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = BuildContainer(db, executor);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RunEdgeUpdateCycleCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(EdgeUpdateStatus.Succeeded));

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(i => i.Id == installationId);
        persisted.Version.Should().Be("1.5.0");
        persisted.TargetVersion.Should().BeNull();
        persisted.LastUpdateStatus.Should().Be(nameof(EdgeUpdateStatus.Succeeded));
        executor.RollbackCallCount.Should().Be(0);
    }

    /// <summary>Cenário Gherkin "Rollback automático" (US-146 §4).</summary>
    [Fact]
    public async Task Falha_No_Health_Check_Dispara_Rollback_Automatico_E_Alerta_A_Plataforma()
    {
        var (tenantId, installationId) = await SeedInstallationAsync(CurrentHourWindowJson());
        await SetTargetVersionAsync(tenantId, installationId, "1.5.0");

        var executor = new FakeEdgeUpdateExecutor { HealthCheckSucceeds = false };
        var notifier = new RecordingPlatformAlertNotifier();
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = BuildContainer(db, executor, notifier);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RunEdgeUpdateCycleCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(EdgeUpdateStatus.RolledBack));

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(i => i.Id == installationId);
        persisted.Version.Should().Be("1.4.0", "rollback garante que a instalação continua operante na versão anterior");
        persisted.LastUpdateStatus.Should().Be(nameof(EdgeUpdateStatus.RolledBack));

        executor.RollbackCallCount.Should().Be(1);
        executor.LastRollbackPreviousVersion.Should().Be("1.4.0");

        notifier.EdgeUpdateRolledBackCalls.Should().ContainSingle();
        var call = notifier.EdgeUpdateRolledBackCalls.Single();
        call.InstallationId.Should().Be(installationId);
        call.TargetVersion.Should().Be("1.5.0");
        call.PreviousVersion.Should().Be("1.4.0");
    }

    [Fact]
    public async Task Falha_No_Backup_Marca_Failed_Sem_Tentar_Download_Ou_Migration()
    {
        var (tenantId, installationId) = await SeedInstallationAsync(CurrentHourWindowJson());
        await SetTargetVersionAsync(tenantId, installationId, "1.5.0");

        var executor = new FakeEdgeUpdateExecutor { BackupSucceeds = false };
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = BuildContainer(db, executor);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RunEdgeUpdateCycleCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(EdgeUpdateStatus.Failed));

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(i => i.Id == installationId);
        persisted.Version.Should().Be("1.4.0");
        persisted.LastUpdateStatus.Should().Be(nameof(EdgeUpdateStatus.Failed));
        executor.RollbackCallCount.Should().Be(0, "sem migration aplicada ainda, não há o que reverter");
    }

    private static string CurrentHourWindowJson()
    {
        var now = DateTimeOffset.UtcNow;
        var end = (now.Hour + 1) % 24;
        return $$"""{"updateWindowStartHour":{{now.Hour}},"updateWindowEndHour":{{end}}}""";
    }

    private static string OutsideCurrentHourWindowJson()
    {
        var now = DateTimeOffset.UtcNow;
        var start = (now.Hour + 3) % 24;
        var end = (now.Hour + 4) % 24;
        return $$"""{"updateWindowStartHour":{{start}},"updateWindowEndHour":{{end}}}""";
    }

    private async Task<(Guid TenantId, Guid InstallationId)> SeedInstallationAsync(string maintenanceJson)
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        Guid installationId;
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            db.TenantConfigs.Add(TenantConfig.Create(tenantId));

            var store = Store.Create(tenantId, "Matriz", isDefault: true);
            db.Stores.Add(store);

            var installation = EdgeInstallation.CreateInstalled(
                Guid.NewGuid(), tenantId, store.Id, "Servidor local — Matriz", publicKey: "pk-teste", version: "1.4.0");
            db.EdgeInstallations.Add(installation);

            await db.SaveChangesAsync();
            installationId = installation.Id;
        }

        await using (var db = (AppDbContext)_fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE tenant_config SET maintenance = {maintenanceJson}::jsonb WHERE tenant_id = {tenantId}");
        }

        return (tenantId, installationId);
    }

    private async Task SetTargetVersionAsync(Guid tenantId, Guid installationId, string targetVersion)
    {
        await using var db = (AppDbContext)_fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE edge_installation SET target_version = {targetVersion} WHERE id = {installationId}");
    }

    private async Task SeedPendingOutboxAsync(Guid tenantId, int count)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        for (var i = 0; i < count; i++)
        {
            db.OutboxEntries.Add(Nexora.Domain.Sync.Outbox.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, tenantId, deviceSeq: i + 1));
        }

        await db.SaveChangesAsync();
    }

    private static ServiceProvider BuildContainer(
        IApplicationDbContext db, IEdgeUpdateExecutor executor, IPlatformAlertNotifier? notifier = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(executor);
        services.AddSingleton(notifier ?? new RecordingPlatformAlertNotifier());
        // LoggingBehavior<,> exige ICurrentTenantContext só para logar tenantId/storeId — o
        // isolamento de RLS de verdade já vem do AppDbContext passado acima (construído com o
        // StaticTenantContext certo no próprio teste); este registro aqui é só para satisfazer o
        // pipeline MediatR, mesmo espírito de PollSyncHealthIntegrationTests.BuildContainer.
        services.AddSingleton<ICurrentTenantContext>(new StaticTenantContext(tenantId: null));

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
}
