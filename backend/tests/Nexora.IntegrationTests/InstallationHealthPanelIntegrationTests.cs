using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Installations.Commands.EvaluateInstallationHealth;
using Nexora.Application.Installations.Queries.Platform.GetInstallationDiagnostics;
using Nexora.Application.Installations.Queries.Platform.ListInstallationIncidents;
using Nexora.Application.Installations.Queries.Platform.ListPlatformInstallations;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Devices;
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
/// US-140 "Painel de instalações com saúde" contra Postgres real (Testcontainers) — cobre a
/// estratégia de teste do §12 do doc.: (a) o painel de plataforma lista instalações de VÁRIOS
/// tenants sem vazar dado de negócio (RN-015), (b) instalação sem contato recente é classificada
/// DOWN e abre <see cref="InstallationIncident"/> automaticamente (§9 "a detecção é da nuvem"), (c)
/// isolamento — diagnóstico/histórico de uma instalação nunca expõe linha de outro tenant.
/// </summary>
[Collection("Postgres")]
public sealed class InstallationHealthPanelIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public InstallationHealthPanelIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário "Visão consolidada" (US-140 §4) — 2 tenants, cada um com sua instalação, aparecem na mesma lista.</summary>
    [Fact]
    public async Task ListPlatformInstallations_Cobre_Varios_Tenants_Sem_Vazar_Dado_De_Negocio()
    {
        var tenantA = await SeedInstalledTenantAsync("Pizzaria A", "Matriz A");
        var tenantB = await SeedInstalledTenantAsync("Pizzaria B", "Matriz B");

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = BuildContainer(db);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ListPlatformInstallationsQuery());

        result.IsSuccess.Should().BeTrue();
        var data = result.Value!.Data;

        data.Should().Contain(i => i.InstallationId == tenantA.InstallationId && i.TenantName == "Pizzaria A" && i.StoreName == "Matriz A");
        data.Should().Contain(i => i.InstallationId == tenantB.InstallationId && i.TenantName == "Pizzaria B" && i.StoreName == "Matriz B");

        // RN-015: o contrato (PlatformInstallationSummaryResponse) só carrega identificação e saúde
        // técnica — não existe campo capaz de vazar pedido/pagamento/cliente por construção do tipo.
        var rowA = data.Single(i => i.InstallationId == tenantA.InstallationId);
        rowA.Health.Should().BeOneOf("OK", "DEGRADED", "DOWN");
        rowA.TenantId.Should().Be(tenantA.TenantId);
    }

    /// <summary>Cenário "Instalação fora do ar" (US-140 §4) — sem contato há mais que o limiar vira DOWN e abre incidente.</summary>
    [Fact]
    public async Task Instalacao_Sem_Contato_Recente_E_Classificada_Down_E_Abre_Incidente_E_Notifica_A_Plataforma()
    {
        var seeded = await SeedInstalledTenantAsync("Pizzaria Down", "Matriz");
        await BackdateLastSeenAsync(seeded.InstallationId, DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30), seeded.TenantId);

        var tenantContext = new StaticTenantContext(seeded.TenantId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var notifier = new RecordingPlatformAlertNotifier();
        await using var provider = BuildContainer(db, notifier);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new EvaluateInstallationHealthCommand(seeded.TenantId, seeded.InstallationId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Health.Should().Be("DOWN");
        result.Value!.TransitionOccurred.Should().BeTrue();

        var incident = await db.InstallationIncidents.AsNoTracking()
            .SingleAsync(i => i.InstallationId == seeded.InstallationId);
        incident.Type.Should().Be(InstallationIncidentType.Offline);
        incident.IsOpen.Should().BeTrue();
        incident.Cause.Should().NotBeNullOrWhiteSpace();

        (await db.DomainEvents.AsNoTracking().AnyAsync(e =>
                e.TenantId == seeded.TenantId && e.AggregateId == seeded.InstallationId && e.Type == "installation.health_down"))
            .Should().BeTrue();

        notifier.Calls.Should().ContainSingle();
        notifier.Calls.Single().InstallationId.Should().Be(seeded.InstallationId);

        // Reavaliar sem nova mudança de estado não deve abrir um segundo incidente (idempotência
        // da avaliação periódica do worker).
        var second = await sender.Send(new EvaluateInstallationHealthCommand(seeded.TenantId, seeded.InstallationId));
        second.Value!.TransitionOccurred.Should().BeFalse();
        (await db.InstallationIncidents.AsNoTracking().CountAsync(i => i.InstallationId == seeded.InstallationId)).Should().Be(1);
    }

    /// <summary>Instalação volta a ter contato recente — o incidente aberto é resolvido (não fica pendurado).</summary>
    [Fact]
    public async Task Instalacao_Que_Volta_A_Sincronizar_Resolve_O_Incidente_Aberto()
    {
        var seeded = await SeedInstalledTenantAsync("Pizzaria Recovery", "Matriz");
        await BackdateLastSeenAsync(seeded.InstallationId, DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30), seeded.TenantId);

        var tenantContext = new StaticTenantContext(seeded.TenantId);

        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        await using (var provider = BuildContainer(db))
        {
            var sender = provider.GetRequiredService<ISender>();
            await sender.Send(new EvaluateInstallationHealthCommand(seeded.TenantId, seeded.InstallationId));
        }

        // Volta a sincronizar agora.
        await BackdateLastSeenAsync(seeded.InstallationId, DateTimeOffset.UtcNow, seeded.TenantId);

        // Novo DbContext/scope (mesmo espírito do worker real, que abre um IServiceScope novo a
        // cada varredura — ver InstallationHealthEvaluationWorker.RunOnceAsync): reusar a MESMA
        // instância acima devolveria a entidade já rastreada com o LastSeenAt antigo (identity map
        // do EF Core), o que não reproduz como o comando roda de verdade em produção (um
        // EvaluateInstallationHealthCommand por vez, cada um numa oportunidade de leitura fresca).
        await using var recoveryDb = _fixture.CreateAppDbContext(tenantContext);
        await using var recoveryProvider = BuildContainer(recoveryDb);
        var recoverySender = recoveryProvider.GetRequiredService<ISender>();

        var recovered = await recoverySender.Send(new EvaluateInstallationHealthCommand(seeded.TenantId, seeded.InstallationId));

        recovered.Value!.Health.Should().Be("OK");
        recovered.Value!.TransitionOccurred.Should().BeTrue();

        var incident = await recoveryDb.InstallationIncidents.AsNoTracking().SingleAsync(i => i.InstallationId == seeded.InstallationId);
        incident.IsOpen.Should().BeFalse();
        incident.ResolvedAt.Should().NotBeNull();
    }

    /// <summary>Cenário "Diagnóstico remoto" + isolamento (US-140 §4/§12) — nunca expõe evento/incidente de outro tenant, nem dado de negócio do próprio.</summary>
    [Fact]
    public async Task Diagnostico_E_Historico_So_Expoem_A_Propria_Instalacao_Sem_Dado_De_Negocio()
    {
        var tenantA = await SeedInstalledTenantAsync("Pizzaria Isolada A", "Matriz");
        var tenantB = await SeedInstalledTenantAsync("Pizzaria Isolada B", "Matriz");
        await BackdateLastSeenAsync(tenantA.InstallationId, DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30), tenantA.TenantId);

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA.TenantId)))
        {
            // Evento de NEGÓCIO do mesmo tenant (aggregateType "order") — RN-015 exige que o
            // diagnóstico/log da instalação NUNCA misture isso, mesmo estando no mesmo tenant.
            seedDb.DomainEvents.Add(DomainEvent.Create(
                tenantA.TenantId, type: "order.created", aggregateType: "order", aggregateId: Guid.NewGuid(),
                payload: """{"total":"42.00"}""", origin: "EDGE", occurredAt: DateTimeOffset.UtcNow));
            await seedDb.SaveChangesAsync();
        }

        var tenantContextA = new StaticTenantContext(tenantA.TenantId);
        await using var dbA = _fixture.CreateAppDbContext(tenantContextA);
        await using var providerA = BuildContainer(dbA);
        var senderA = providerA.GetRequiredService<ISender>();

        await senderA.Send(new EvaluateInstallationHealthCommand(tenantA.TenantId, tenantA.InstallationId));

        var diagnostics = await senderA.Send(new GetInstallationDiagnosticsQuery(tenantA.InstallationId));
        diagnostics.IsSuccess.Should().BeTrue();
        diagnostics.Value!.RecentLogs.Should().NotBeEmpty();
        diagnostics.Value!.RecentLogs.Should().OnlyContain(log =>
            !log.Message.Contains("42.00") && !log.Message.Contains("order.created"),
            "RN-015: log da instalação nunca deve carregar evento de negócio do tenant");

        var incidents = await senderA.Send(new ListInstallationIncidentsQuery(tenantA.InstallationId));
        incidents.IsSuccess.Should().BeTrue();
        incidents.Value!.Data.Should().ContainSingle(i => i.Type == "OFFLINE");

        // Instalação de outro tenant consultada pelo MESMO sender (nenhum ICurrentTenantContext de
        // requisição HTTP limita a busca — PlatformInstallationLookup varre plataforma inteira de
        // propósito) ainda assim só retorna o histórico DELA, nunca misturado com o de tenantA.
        var incidentsB = await senderA.Send(new ListInstallationIncidentsQuery(tenantB.InstallationId));
        incidentsB.IsSuccess.Should().BeTrue();
        incidentsB.Value!.Data.Should().NotContain(i => i.Type == "OFFLINE");

        // Id inexistente não vaza nem revela nada — Result de falha, mesma convenção ADR-021.
        var notFound = await senderA.Send(new GetInstallationDiagnosticsQuery(Guid.NewGuid()));
        notFound.IsFailure.Should().BeTrue();
    }

    private async Task<(Guid TenantId, Guid StoreId, Guid InstallationId)> SeedInstalledTenantAsync(string tenantName, string storeName)
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", tenantName));
            await db.SaveChangesAsync();
        }

        Guid storeId;
        Guid installationId;
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var store = Store.Create(tenantId, storeName, isDefault: true);
            db.Stores.Add(store);

            var installation = EdgeInstallation.CreateInstalled(
                Guid.NewGuid(), tenantId, store.Id, $"Servidor local — {storeName}", publicKey: "pk-teste", version: "1.4.2");
            db.EdgeInstallations.Add(installation);

            await db.SaveChangesAsync();
            storeId = store.Id;
            installationId = installation.Id;
        }

        // Instalação recém-criada começa com contato recente (agora) para os testes que não
        // precisam simular defasagem — BackdateLastSeenAsync ajusta quando o cenário exige.
        await BackdateLastSeenAsync(installationId, DateTimeOffset.UtcNow, tenantId);

        return (tenantId, storeId, installationId);
    }

    /// <summary>
    /// <see cref="EdgeInstallation.RecordHeartbeat"/> só grava <c>DateTimeOffset.UtcNow</c> — não há
    /// forma pelo Domain de simular "sem contato há 30 minutos" sem um relógio injetável (fora do
    /// escopo desta história). Ajusta a coluna diretamente, mesmo mecanismo de qualquer seed de
    /// teste que precise de um estado passado que o agregado não expõe como parâmetro.
    /// </summary>
    private async Task BackdateLastSeenAsync(Guid installationId, DateTimeOffset lastSeenAt, Guid? tenantId = null)
    {
        await using var db = _fixture.CreateAppDbContext(tenantId is { } t ? new StaticTenantContext(t) : null);
        var appDb = (AppDbContext)db;
        await appDb.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE edge_installation SET last_seen_at = {lastSeenAt} WHERE id = {installationId}");
    }

    private static ServiceProvider BuildContainer(IApplicationDbContext db, RecordingPlatformAlertNotifier? notifier = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton<ICurrentTenantContext>(new StaticTenantContext(tenantId: null));
        services.AddSingleton<IEventOriginProvider, CloudEventOriginProvider>();
        services.AddSingleton<IAppVersionProvider>(new FixedAppVersionProvider("1.4.2"));
        services.AddSingleton<IPlatformAlertNotifier>(notifier ?? new RecordingPlatformAlertNotifier());

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
