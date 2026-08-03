using System.Text.Json;
using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Installation.Abstractions;
using Nexora.Application.Installation.Commands.PollSyncHealth;
using Nexora.Domain.Platform;
using Nexora.Domain.Sync;
using Nexora.Infrastructure.Devices;
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
/// US-034 §6/§12 ("Integração: detecção de queda e de retorno emitindo os eventos corretos" /
/// "nenhum evento se perde durante a queda; contagem no outbox bate com a operação") —
/// <c>PollSyncHealthCommandHandler</c> contra Postgres real (Testcontainers, mesma
/// <see cref="PostgresFixture"/> das demais suites), simulando a sequência "nuvem OK -&gt; cai -&gt;
/// volta" via <see cref="SequencedSyncHealthPoller"/> (o real <c>SyncHealthPoller</c> faz HTTP de
/// verdade, fora do que este teste precisa exercitar — a REGRA de transição é o objeto do teste,
/// não o transporte HTTP).
/// </summary>
[Collection("Postgres")]
public sealed class PollSyncHealthIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public PollSyncHealthIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Detecção de perda de conexão" (US-034 §4).</summary>
    [Fact]
    public async Task Queda_Da_Nuvem_Emite_Edge_Offline_Detected_Com_LastSyncAt_E_PendingEvents()
    {
        var (tenantId, storeId, installationId) = await SeedInstallationAsync();

        var tenantContext = new StaticTenantContext(tenantId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var poller = new SequencedSyncHealthPoller();
        var broadcaster = new RecordingSyncStatusBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, poller, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        // Baseline ANTES de semear — outbox é deliberadamente single-tenant/sem RLS no schema
        // real (edge só atende uma loja, ver comentário de EnableRowLevelSecurity), então este
        // container Postgres compartilhado entre suites pode ter linhas de outros testes. O
        // handler em produção também conta sem filtrar por tenant (mesma leitura de
        // GetInstallationHealthQueryHandler) — daí comparar contra baseline + delta, não um
        // número absoluto, para o teste não depender da ordem de execução das outras suites.
        var baselinePending = await CountPendingOutboxAsync(db);

        // A produção continua enfileirando eventos normalmente ANTES de qualquer poll — a
        // detecção de conectividade não tem nada a ver com o outbox em si (ADR-007).
        await SeedOutboxEntriesAsync(db, tenantId, count: 3);
        var expectedPending = baselinePending + 3;

        // 1º poll: nuvem OK — só estabelece o estado inicial (Unknown -> Online); NUNCA é
        // tratado como "reconexão" (não havia queda nenhuma para retornar de).
        poller.Next = new SyncHealthPollResult(DependencyStatus.Ok, 200);
        var first = await sender.Send(new PollSyncHealthCommand());
        first.IsSuccess.Should().BeTrue();

        broadcaster.Calls.Should().BeEmpty("Unknown -> Online não é uma transição a anunciar, só a primeira observação");

        // 2º poll: nuvem cai — Online -> Offline dispara edge.offline_detected (EVT-083).
        poller.Next = new SyncHealthPollResult(DependencyStatus.Down, null);
        var second = await sender.Send(new PollSyncHealthCommand());
        second.IsSuccess.Should().BeTrue();

        var domainEvent = await db.DomainEvents.AsNoTracking()
            .SingleAsync(e => e.TenantId == tenantId && e.Type == "edge.offline_detected");
        domainEvent.AggregateId.Should().Be(installationId);
        domainEvent.AggregateType.Should().Be("edge_installation");
        domainEvent.StoreId.Should().Be(storeId);
        GetJsonInt(domainEvent.Payload, "pendingEvents").Should().Be(expectedPending);
        HasJsonProperty(domainEvent.Payload, "lastSyncAt").Should().BeTrue();

        broadcaster.Calls.Should().ContainSingle();
        var call = broadcaster.Calls.Single();
        call.TenantId.Should().Be(tenantId);
        call.Online.Should().BeFalse();
        call.PendingEvents.Should().Be(expectedPending);

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(e => e.Id == installationId);
        persisted.Connectivity.Should().Be(SyncConnectivity.Offline);
        persisted.OfflineSince.Should().NotBeNull();

        // Outbox intacto — a detecção de queda nunca toca a fila, só CONTA (ADR-007 continua
        // sendo o único dono da escrita/leitura do outbox).
        (await CountPendingOutboxAsync(db)).Should().Be(expectedPending);
    }

    /// <summary>Cenário Gherkin "Retorno da conexão" (US-034 §4) — "edge offline há 40 minutos com 380 eventos acumulados".</summary>
    [Fact]
    public async Task Retorno_Da_Nuvem_Emite_Edge_Reconnected_Com_OfflineSeconds_E_PendingEvents()
    {
        var (tenantId, storeId, installationId) = await SeedInstallationAsync();

        var tenantContext = new StaticTenantContext(tenantId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var poller = new SequencedSyncHealthPoller();
        var broadcaster = new RecordingSyncStatusBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, poller, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        // Baseline ANTES de semear — ver comentário equivalente no teste de queda.
        var baselinePending = await CountPendingOutboxAsync(db);

        await SeedOutboxEntriesAsync(db, tenantId, count: 5);

        poller.Next = new SyncHealthPollResult(DependencyStatus.Ok, 200);
        await sender.Send(new PollSyncHealthCommand());

        poller.Next = new SyncHealthPollResult(DependencyStatus.Down, null);
        await sender.Send(new PollSyncHealthCommand());

        // Mais eventos entram na fila DURANTE a queda — a operação segue normalmente, sem
        // interrupção (US-034 §3.1/§4: "nenhuma funcionalidade operacional deve ficar bloqueada").
        await SeedOutboxEntriesAsync(db, tenantId, count: 2);
        var expectedPending = baselinePending + 7;

        poller.Next = new SyncHealthPollResult(DependencyStatus.Ok, 200);
        var reconnected = await sender.Send(new PollSyncHealthCommand());
        reconnected.IsSuccess.Should().BeTrue();

        var domainEvent = await db.DomainEvents.AsNoTracking()
            .SingleAsync(e => e.TenantId == tenantId && e.Type == "edge.reconnected");
        domainEvent.AggregateId.Should().Be(installationId);
        domainEvent.AggregateType.Should().Be("edge_installation");
        domainEvent.StoreId.Should().Be(storeId);
        GetJsonInt(domainEvent.Payload, "pendingEvents").Should().Be(expectedPending);
        HasJsonProperty(domainEvent.Payload, "offlineSeconds").Should().BeTrue();

        broadcaster.Calls.Should().HaveCount(2, "uma chamada na queda, outra no retorno");
        var reconnectedCall = broadcaster.Calls.Last();
        reconnectedCall.Online.Should().BeTrue();
        reconnectedCall.PendingEvents.Should().Be(expectedPending);

        var persisted = await db.EdgeInstallations.AsNoTracking().SingleAsync(e => e.Id == installationId);
        persisted.Connectivity.Should().Be(SyncConnectivity.Online);
        persisted.OfflineSince.Should().BeNull(
            "a reconexão limpa OfflineSince — GET /v1/health não deve mais reportar 'desde quando' depois de reconectar");

        // "nenhum evento se perde durante a queda; contagem no outbox bate com a operação" (US-034 §12).
        (await CountPendingOutboxAsync(db)).Should().Be(expectedPending);
    }

    /// <summary>Todo poll sem MUDANÇA de estado (nuvem sempre alcançável) não deve gerar ruído nenhum.</summary>
    [Fact]
    public async Task Poll_Sem_Transicao_Real_Nao_Emite_Evento_Nem_Broadcast()
    {
        var (tenantId, _, _) = await SeedInstallationAsync();

        var tenantContext = new StaticTenantContext(tenantId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var poller = new SequencedSyncHealthPoller();
        var broadcaster = new RecordingSyncStatusBroadcaster();
        await using var provider = BuildContainer(db, tenantContext, poller, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        poller.Next = new SyncHealthPollResult(DependencyStatus.Ok, 200);
        await sender.Send(new PollSyncHealthCommand());
        await sender.Send(new PollSyncHealthCommand());
        await sender.Send(new PollSyncHealthCommand());

        broadcaster.Calls.Should().BeEmpty("nuvem sempre OK — nenhuma transição para anunciar");
        (await db.DomainEvents.AsNoTracking().CountAsync(e => e.TenantId == tenantId)).Should().Be(0);
    }

    private async Task<(Guid TenantId, Guid StoreId, Guid InstallationId)> SeedInstallationAsync()
    {
        var tenantId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        Guid storeId;
        Guid installationId;
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var store = Store.Create(tenantId, "Matriz", isDefault: true, timezone: "America/Sao_Paulo");
            db.Stores.Add(store);

            var installation = EdgeInstallation.CreateInstalled(
                Guid.NewGuid(), tenantId, store.Id, "Servidor local — Matriz", publicKey: "pk-teste", version: "1.0.0");
            db.EdgeInstallations.Add(installation);

            await db.SaveChangesAsync();
            storeId = store.Id;
            installationId = installation.Id;
        }

        return (tenantId, storeId, installationId);
    }

    private static async Task SeedOutboxEntriesAsync(IApplicationDbContext db, Guid tenantId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            db.OutboxEntries.Add(Outbox.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, tenantId, deviceSeq: i + 1));
        }

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private static Task<int> CountPendingOutboxAsync(IApplicationDbContext db) =>
        db.OutboxEntries.AsNoTracking().CountAsync(o => o.Status != "SYNCED");

    /// <summary>Lê um inteiro do payload JSON sem depender de como o serializer formata espaços/ordem de campos.</summary>
    private static int GetJsonInt(string json, string property)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(property).GetInt32();
    }

    private static bool HasJsonProperty(string json, string property)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty(property, out _);
    }

    private static ServiceProvider BuildContainer(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        ISyncHealthPoller poller,
        ISyncStatusBroadcaster broadcaster)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton(poller);
        services.AddSingleton(broadcaster);

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
