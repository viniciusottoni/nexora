using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Stations.Commands.CreateStation;
using Nexora.Application.Stations.Commands.DeleteStation;
using Nexora.Application.Stations.Commands.UpdateStation;
using Nexora.Application.Stations.Queries.ListStations;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin da US-017 (Cadastro de praças de produção) contra um PostgreSQL real
/// (Testcontainers, mesma <see cref="PostgresFixture"/> das demais suites) e o pipeline MediatR de
/// produção (Validation -&gt; Logging -&gt; Transaction, via <see cref="MediatRTestContainerFactory"/>)
/// — mesmo padrão de <c>DevicesIntegrationTests</c> ("handler completo contra Postgres real").
/// </summary>
[Collection("Postgres")]
public sealed class StationsIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public StationsIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cria uma praça e a contagem de produtos vinculados aparece zerada na listagem (US-017 §10).</summary>
    [Fact]
    public async Task Criar_Praca_E_Listar_Devolve_A_Praca_Criada_Com_Contagem_De_Produtos_Zerada()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, storeId, managerId));
        var sender = provider.GetRequiredService<ISender>();

        var createResult = await sender.Send(new CreateStationCommand("OVEN", "Forno", "red", CapacitySlots: 5, IsBottleneck: true, Position: 1));

        createResult.IsSuccess.Should().BeTrue();
        createResult.Value!.Code.Should().Be("OVEN");
        createResult.Value!.IsBottleneck.Should().BeTrue();
        createResult.Value!.LinkedProductCount.Should().Be(0);

        var listResult = await sender.Send(new ListStationsQuery());

        listResult.IsSuccess.Should().BeTrue();
        listResult.Value!.Items.Should().ContainSingle(s => s.Id == createResult.Value!.Id && s.LinkedProductCount == 0);

        var domainEvent = await db.DomainEvents.SingleAsync(e => e.TenantId == tenantId && e.Type == "tenant.config_updated");
        domainEvent.AggregateId.Should().Be(createResult.Value!.Id);

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.TenantId == tenantId && a.Action == "STATION_CREATED");
        auditEntry.EntityId.Should().Be(createResult.Value!.Id);
    }

    /// <summary>Cenário Gherkin adaptado de "Praças padrão do modelo pizzaria": só uma praça pode ser o gargalo por vez.</summary>
    [Fact]
    public async Task Criar_Segunda_Praca_Como_Gargalo_Desmarca_A_Primeira()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, storeId, managerId));
        var sender = provider.GetRequiredService<ISender>();

        var first = await sender.Send(new CreateStationCommand("OVEN", "Forno", null, 5, IsBottleneck: true, Position: 1));
        first.IsSuccess.Should().BeTrue();

        var second = await sender.Send(new CreateStationCommand("GRILL", "Chapa", null, 3, IsBottleneck: true, Position: 2));
        second.IsSuccess.Should().BeTrue();

        var firstStation = await db.Stations.SingleAsync(s => s.Id == first.Value!.Id);
        var secondStation = await db.Stations.SingleAsync(s => s.Id == second.Value!.Id);

        firstStation.IsBottleneck.Should().BeFalse("o gargalo é, por definição, um só — a segunda marcação desmarca a primeira");
        secondStation.IsBottleneck.Should().BeTrue();
    }

    /// <summary>Mesma regra de exclusividade do gargalo, agora via PATCH (UpdateStationCommand).</summary>
    [Fact]
    public async Task Marcar_Praca_Existente_Como_Gargalo_Via_Update_Desmarca_A_Anterior()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, storeId, managerId));
        var sender = provider.GetRequiredService<ISender>();

        var oven = await sender.Send(new CreateStationCommand("OVEN", "Forno", null, 5, IsBottleneck: true, Position: 1));
        var grill = await sender.Send(new CreateStationCommand("GRILL", "Chapa", null, 3, IsBottleneck: false, Position: 2));

        var updateResult = await sender.Send(new UpdateStationCommand(grill.Value!.Id, Name: null, Color: null, CapacitySlots: null, IsBottleneck: true, Position: null));

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value!.IsBottleneck.Should().BeTrue();

        var ovenStation = await db.Stations.SingleAsync(s => s.Id == oven.Value!.Id);
        ovenStation.IsBottleneck.Should().BeFalse();
    }

    /// <summary>Cenário Gherkin "Exclusão de praça com produtos vinculados" (US-017 §4).</summary>
    [Fact]
    public async Task Excluir_Praca_Com_Produtos_Vinculados_E_Recusado()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, storeId, managerId));
        var sender = provider.GetRequiredService<ISender>();

        var stationResult = await sender.Send(new CreateStationCommand("OVEN", "Forno", null, 5, IsBottleneck: false, Position: 1));
        var stationId = stationResult.Value!.Id;

        var category = Category.Create(tenantId, "Pizzas");
        db.Categories.Add(category);
        var product = Product.Create(tenantId, category.Id, "Calabresa", stationId: stationId);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var deleteResult = await sender.Send(new DeleteStationCommand(stationId));

        deleteResult.IsSuccess.Should().BeFalse();
        deleteResult.Code.Should().Be(ApiErrorCodes.StationHasLinkedProducts);

        (await db.Stations.SingleAsync(s => s.Id == stationId)).DeletedAt.Should().BeNull("a exclusão recusada não deve alterar a praça");
    }

    /// <summary>Depois de reatribuir/remover o produto vinculado, a exclusão passa a ser aceita.</summary>
    [Fact]
    public async Task Excluir_Praca_Sem_Produtos_Vinculados_E_Aceito()
    {
        var (tenantId, storeId) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, storeId, managerId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, storeId, managerId));
        var sender = provider.GetRequiredService<ISender>();

        var stationResult = await sender.Send(new CreateStationCommand("BAR", "Bebidas", null, null, IsBottleneck: false, Position: 1));

        var deleteResult = await sender.Send(new DeleteStationCommand(stationResult.Value!.Id));

        deleteResult.IsSuccess.Should().BeTrue();
        (await db.Stations.SingleAsync(s => s.Id == stationResult.Value!.Id)).DeletedAt.Should().NotBeNull();
    }

    /// <summary>Isolamento multi-tenant (RLS, ADR-004) no fluxo de criação/listagem de praças — DoD da US-017.</summary>
    [Fact]
    public async Task Listagem_De_Pracas_Nao_Vaza_Entre_Tenants()
    {
        var (tenantA, storeA) = await SeedTenantAndStoreAsync();
        var (tenantB, storeB) = await SeedTenantAndStoreAsync();
        var managerId = Guid.NewGuid();

        await using (var dbA = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA, storeA, managerId)))
        await using (var providerA = MediatRTestContainerFactory.Build(dbA, new StaticTenantContext(tenantA, storeA, managerId)))
        {
            var result = await providerA.GetRequiredService<ISender>()
                .Send(new CreateStationCommand("OVEN", "Forno", null, 5, IsBottleneck: true, Position: 1));
            result.IsSuccess.Should().BeTrue();
        }

        await using var dbB = _fixture.CreateAppDbContext(new StaticTenantContext(tenantB, storeB, managerId));
        await using var providerB = MediatRTestContainerFactory.Build(dbB, new StaticTenantContext(tenantB, storeB, managerId));

        var listB = await providerB.GetRequiredService<ISender>().Send(new ListStationsQuery());

        listB.IsSuccess.Should().BeTrue();
        listB.Value!.Items.Should().BeEmpty("o RLS (ADR-004) impede que a praça do tenant A apareça na listagem do tenant B");
    }

    private async Task<(Guid TenantId, Guid StoreId)> SeedTenantAndStoreAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        await using var storeDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        storeDb.Stores.Add(Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));
        await storeDb.SaveChangesAsync();

        return (tenantId, storeId);
    }
}
