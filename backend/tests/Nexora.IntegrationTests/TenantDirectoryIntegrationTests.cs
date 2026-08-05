using Nexora.Application.Tenants.Queries.ListTenants;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-151 §12 "Integração: busca combinada, paginação estável e normalização de status" — contra
/// Postgres real (Testcontainers). <c>tenant</c> não tem RLS (raiz global), então o container é
/// compartilhado com TODA a suíte de integração (<see cref="PostgresFixture"/>/<c>PostgresCollection</c>)
/// e outros testes também semeiam tenants ali — por isso cada teste aqui usa um marcador
/// (GUID embutido no nome/e-mail) só seu como termo de busca, nunca conta linhas "soltas" sem
/// filtrar por esse marcador.
/// </summary>
[Collection("Postgres")]
public sealed class TenantDirectoryIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public TenantDirectoryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Busca_Combinada_De_Nome_E_Status_Retorna_Somente_A_Correspondencia_Esperada()
    {
        var marker = UniqueMarker();
        var betinhaActive = await SeedTenantAsync($"Pizzaria {marker} Betinha", TenantStatus.Active, ownerEmail: $"dono-{marker}@example.com");
        await SeedTenantAsync($"{marker} Betinha Delivery", TenantStatus.Provisioned);
        await SeedTenantAsync($"Hamburgueria {marker} do Zé", TenantStatus.Active);

        var response = await SendQueryAsync(new ListTenantsQuery(
            SearchTerm: $"{marker} betinha",
            Statuses: new[] { TenantStatus.Active },
            Plans: Array.Empty<string>(),
            Templates: Array.Empty<string>(),
            HealthStatuses: Array.Empty<TenantHealthStatus>(),
            CreatedFrom: null,
            CreatedTo: null,
            Sort: TenantDirectorySort.Attention,
            Limit: 50,
            Cursor: null));

        response.Data.Should().ContainSingle().Which.Id.Should().Be(betinhaActive);
        response.AppliedFilters.Query.Should().Be($"{marker} betinha");
        response.AppliedFilters.Status.Should().Equal("ACTIVE");
    }

    [Fact]
    public async Task Busca_Encontra_Por_Email_Do_Proprietario()
    {
        var marker = UniqueMarker();
        var ownerEmail = $"dono-{marker}@example.com";
        var tenantId = await SeedTenantAsync($"Estabelecimento {marker}", TenantStatus.Active, ownerEmail: ownerEmail);

        var response = await SendQueryAsync(new ListTenantsQuery(
            SearchTerm: ownerEmail,
            Statuses: Array.Empty<TenantStatus>(),
            Plans: Array.Empty<string>(),
            Templates: Array.Empty<string>(),
            HealthStatuses: Array.Empty<TenantHealthStatus>(),
            CreatedFrom: null,
            CreatedTo: null,
            Sort: TenantDirectorySort.Attention,
            Limit: 50,
            Cursor: null));

        response.Data.Should().ContainSingle().Which.Id.Should().Be(tenantId);
    }

    [Fact]
    public async Task Status_E_Saude_Vem_Sempre_Em_Caixa_Alta_E_Sem_Instalacao_A_Saude_E_Unknown()
    {
        var marker = UniqueMarker();
        await SeedTenantAsync($"Estabelecimento Suspenso {marker}", TenantStatus.Suspended);

        var response = await SendQueryAsync(new ListTenantsQuery(
            SearchTerm: marker,
            Statuses: Array.Empty<TenantStatus>(),
            Plans: Array.Empty<string>(),
            Templates: Array.Empty<string>(),
            HealthStatuses: Array.Empty<TenantHealthStatus>(),
            CreatedFrom: null,
            CreatedTo: null,
            Sort: TenantDirectorySort.Attention,
            Limit: 50,
            Cursor: null));

        var entry = response.Data.Should().ContainSingle().Subject;
        entry.Status.Should().Be("SUSPENDED"); // nunca "Suspended" (enum C# cru)
        entry.Health.Should().Be("UNKNOWN"); // sem instalação instalada — nunca "DOWN" por engano
        entry.StoresCount.Should().Be(0);
        entry.InstallationsCount.Should().Be(0);
    }

    [Fact]
    public async Task Ordenacao_Por_Criticidade_Traz_Suspenso_Antes_De_Ativo()
    {
        var marker = UniqueMarker();
        var active = await SeedTenantAsync($"Ativo {marker}", TenantStatus.Active);
        var suspended = await SeedTenantAsync($"Suspenso {marker}", TenantStatus.Suspended);

        var response = await SendQueryAsync(new ListTenantsQuery(
            SearchTerm: marker,
            Statuses: Array.Empty<TenantStatus>(),
            Plans: Array.Empty<string>(),
            Templates: Array.Empty<string>(),
            HealthStatuses: Array.Empty<TenantHealthStatus>(),
            CreatedFrom: null,
            CreatedTo: null,
            Sort: TenantDirectorySort.Attention,
            Limit: 50,
            Cursor: null));

        response.Data.Should().HaveCount(2);
        response.Data[0].Id.Should().Be(suspended);
        response.Data[1].Id.Should().Be(active);
    }

    [Fact]
    public async Task Paginacao_Por_Cursor_Nao_Repete_Nem_Pula_Registro()
    {
        var marker = UniqueMarker();
        var seededIds = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            seededIds.Add(await SeedTenantAsync($"Loja {marker} {i}", TenantStatus.Active));
        }

        var collectedIds = new List<Guid>();
        string? cursor = null;

        for (var page = 0; page < 10; page++)
        {
            var response = await SendQueryAsync(new ListTenantsQuery(
                SearchTerm: marker,
                Statuses: Array.Empty<TenantStatus>(),
                Plans: Array.Empty<string>(),
                Templates: Array.Empty<string>(),
                HealthStatuses: Array.Empty<TenantHealthStatus>(),
                CreatedFrom: null,
                CreatedTo: null,
                Sort: TenantDirectorySort.CreatedAt,
                Limit: 2,
                Cursor: cursor));

            collectedIds.AddRange(response.Data.Select(d => d.Id));
            cursor = response.NextCursor;

            if (cursor is null)
            {
                break;
            }
        }

        collectedIds.Should().HaveCount(5);
        collectedIds.Distinct().Should().HaveCount(5, "a paginação por cursor não deve repetir nenhum registro");
        collectedIds.Should().BeEquivalentTo(seededIds, "a paginação por cursor não deve omitir nenhum registro");
    }

    [Fact]
    public async Task Paginacao_Por_Nome_Nao_Repete_Nem_Pula_Registro()
    {
        // sort=name é o único critério cujo keyset compara STRING (t.Name.CompareTo(cursor.Primary))
        // em vez de data/enum — o caminho de tradução mais arriscado para o provider Npgsql, por
        // isso ganha um teste de paginação dedicado (os demais sorts já são cobertos por
        // Paginacao_Por_Cursor_Nao_Repete_Nem_Pula_Registro, que usa CreatedAt).
        var marker = UniqueMarker();
        var seededIds = new List<Guid>();
        foreach (var suffix in new[] { "Alfa", "Beta", "Gama", "Delta", "Epsilon" })
        {
            seededIds.Add(await SeedTenantAsync($"{suffix} {marker}", TenantStatus.Active));
        }

        var collectedIds = new List<Guid>();
        string? cursor = null;

        for (var page = 0; page < 10; page++)
        {
            var response = await SendQueryAsync(new ListTenantsQuery(
                SearchTerm: marker,
                Statuses: Array.Empty<TenantStatus>(),
                Plans: Array.Empty<string>(),
                Templates: Array.Empty<string>(),
                HealthStatuses: Array.Empty<TenantHealthStatus>(),
                CreatedFrom: null,
                CreatedTo: null,
                Sort: TenantDirectorySort.Name,
                Limit: 2,
                Cursor: cursor));

            collectedIds.AddRange(response.Data.Select(d => d.Id));
            cursor = response.NextCursor;

            if (cursor is null)
            {
                break;
            }
        }

        collectedIds.Should().HaveCount(5);
        collectedIds.Distinct().Should().HaveCount(5, "a paginação por cursor não deve repetir nenhum registro");
        collectedIds.Should().BeEquivalentTo(seededIds, "a paginação por cursor não deve omitir nenhum registro");
    }

    [Fact]
    public async Task Filtro_De_Saude_So_Traz_Tenants_Cuja_Instalacao_Agregada_Bate_Com_O_Filtro()
    {
        var marker = UniqueMarker();

        var healthyTenantId = await SeedTenantAsync($"Saudavel {marker}", TenantStatus.Active);
        await SeedInstalledEdgeInstallationAsync(healthyTenantId, lastSeenAt: DateTimeOffset.UtcNow);

        var downTenantId = await SeedTenantAsync($"Fora Do Ar {marker}", TenantStatus.Active);
        await SeedInstalledEdgeInstallationAsync(downTenantId, lastSeenAt: DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30));

        var response = await SendQueryAsync(new ListTenantsQuery(
            SearchTerm: marker,
            Statuses: Array.Empty<TenantStatus>(),
            Plans: Array.Empty<string>(),
            Templates: Array.Empty<string>(),
            HealthStatuses: new[] { TenantHealthStatus.Down },
            CreatedFrom: null,
            CreatedTo: null,
            Sort: TenantDirectorySort.Attention,
            Limit: 50,
            Cursor: null));

        response.Data.Should().ContainSingle().Which.Id.Should().Be(downTenantId);
        response.Data[0].Health.Should().Be("DOWN");
    }

    [Fact]
    public async Task Filtro_Por_Modelo_De_Negocio_Retorna_Somente_O_Template_Pedido()
    {
        var marker = UniqueMarker();
        var pizzaria = await SeedTenantAsync($"Pizzaria {marker}", TenantStatus.Active, templateCode: "PIZZERIA");
        await SeedTenantAsync($"Hamburgueria {marker}", TenantStatus.Active, templateCode: "HAMBURGUERIA");

        var response = await SendQueryAsync(new ListTenantsQuery(
            SearchTerm: marker,
            Statuses: Array.Empty<TenantStatus>(),
            Plans: Array.Empty<string>(),
            Templates: new[] { "PIZZERIA" },
            HealthStatuses: Array.Empty<TenantHealthStatus>(),
            CreatedFrom: null,
            CreatedTo: null,
            Sort: TenantDirectorySort.Attention,
            Limit: 50,
            Cursor: null));

        response.Data.Should().ContainSingle().Which.Id.Should().Be(pizzaria);
        response.AppliedFilters.Template.Should().Equal("PIZZERIA");
    }

    private static string UniqueMarker() => $"qa{Guid.NewGuid():N}"[..14];

    private async Task<Guid> SeedTenantAsync(
        string name, TenantStatus status, string? ownerEmail = null, string? templateCode = null)
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var tenant = Tenant.Create(tenantId, $"tenant-{tenantId:N}", name);

        if (ownerEmail is not null)
        {
            tenant.SetOwnerEmail(ownerEmail);
        }

        if (templateCode is not null)
        {
            tenant.SetTemplateCode(templateCode);
        }

        switch (status)
        {
            case TenantStatus.Active:
                tenant.Activate();
                break;
            case TenantStatus.Suspended:
                tenant.Suspend();
                break;
            case TenantStatus.Cancelled:
                tenant.Cancel();
                break;
        }

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        return tenantId;
    }

    /// <summary>Mesma técnica de <c>PlatformSummaryIntegrationTests.SeedTenantAsync</c>: cria loja + instalação instalada e ajusta <c>last_seen_at</c> via SQL cru (o domínio não expõe um setter para o passado).</summary>
    private async Task SeedInstalledEdgeInstallationAsync(Guid tenantId, DateTimeOffset lastSeenAt)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var store = Store.Create(tenantId, "Matriz", isDefault: true);
        db.Stores.Add(store);

        var installation = EdgeInstallation.CreateInstalled(
            Guid.NewGuid(), tenantId, store.Id, "Servidor local — Matriz", publicKey: "pk-teste", version: "1.4.2");
        db.EdgeInstallations.Add(installation);

        await db.SaveChangesAsync();

        var appDb = (Nexora.Infrastructure.Persistence.AppDbContext)db;
        await appDb.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE edge_installation SET last_seen_at = {lastSeenAt} WHERE id = {installation.Id}");
    }

    private async Task<TenantDirectoryListResponse> SendQueryAsync(ListTenantsQuery query)
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(query);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }
}
