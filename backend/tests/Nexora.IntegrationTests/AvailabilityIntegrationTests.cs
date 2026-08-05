using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Application.Catalog.Availability.Commands.MarkProductAvailable;
using Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;
using Nexora.Application.Catalog.Availability.Commands.RestoreProductsPastBusinessDay;
using Nexora.Application.Catalog.Availability.Queries.ListUnavailableProducts;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
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
/// Cenários Gherkin da US-015 (Marcar produto indisponível com propagação imediata) contra um
/// PostgreSQL real (Testcontainers, mesma <see cref="PostgresFixture"/> das demais suites) e o
/// pipeline MediatR de produção (Validation -&gt; Logging -&gt; Transaction, ADR-037) — mesmo padrão
/// de <c>CatalogIntegrationTests</c>/<c>DevicesIntegrationTests</c>.
///
/// [DESVIO/NOTA DE AMBIENTE] Este worktree isolado só tem Domain+Infrastructure+schema do módulo de
/// catálogo commitados (US-010/US-011 completos — CreateCategoryCommand/CreateProductCommand etc.
/// — existem só como trabalho não commitado no checkout principal, fora do histórico git usado por
/// <c>git worktree add</c>). Por isso categoria/produto são semeados diretamente via
/// <c>Category.Create</c>/<c>Product.Create</c> + <c>SaveChangesAsync</c>, não via comando MediatR
/// (mesmo padrão que <c>DevicesIntegrationTests</c> já usa para Tenant/Store). O container MediatR é
/// montado localmente neste arquivo (<see cref="BuildMediatRContainer"/>), não via
/// <c>MediatRTestContainerFactory</c> compartilhado, para não precisar estender um fixture usado por
/// outras suites com <see cref="IEventOriginProvider"/>/<see cref="IAvailabilityBroadcaster"/> só
/// necessários aqui.
/// </summary>
[Collection("Postgres")]
public sealed class AvailabilityIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AvailabilityIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Propagação imediata" (US-015 §4) — marca indisponível, motivo persistido, evento gravado, broadcast síncrono.</summary>
    [Fact]
    public async Task Marcar_Produto_Indisponivel_Persiste_Motivo_E_Propaga_De_Forma_Sincrona()
    {
        var tenantId = await SeedTenantAsync();
        var (categoryId, productId) = await SeedCategoryAndProductAsync(tenantId, "Pizza Calabresa");
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingAvailabilityBroadcaster();
        await using var provider = BuildMediatRContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new MarkProductUnavailableCommand(productId, "Acabou a calabresa", AutoRestoreNextDay: true));

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAvailable.Should().BeFalse();
        result.Value!.UnavailableReason.Should().Be("Acabou a calabresa");

        var persisted = await db.Products.AsNoTracking().SingleAsync(p => p.Id == productId);
        persisted.IsAvailable.Should().BeFalse();
        persisted.UnavailableReason.Should().Be("Acabou a calabresa");
        persisted.UnavailableSince.Should().NotBeNull();

        // EVT-051 product.availability_changed (US-015 §6), na mesma transação (ADR-006).
        var domainEvent = await db.DomainEvents.SingleAsync(e => e.TenantId == tenantId && e.Type == "product.availability_changed");
        domainEvent.AggregateId.Should().Be(productId);

        // Broadcast síncrono: ao terminar sender.Send, a chamada já está gravada — não foi
        // enfileirada para "depois" (ver docstring de IAvailabilityBroadcaster/RecordingAvailabilityBroadcaster).
        broadcaster.UnavailableCalls.Should().ContainSingle(call => call.ProductId == productId && call.Reason == "Acabou a calabresa");
    }

    /// <summary>Marcar de novo com motivo diferente atualiza o motivo (idempotente, não é erro) e propaga de novo.</summary>
    [Fact]
    public async Task Marcar_Indisponivel_De_Novo_Com_Motivo_Diferente_Atualiza_O_Motivo()
    {
        var tenantId = await SeedTenantAsync();
        var (_, productId) = await SeedCategoryAndProductAsync(tenantId, "Pizza Frango");
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingAvailabilityBroadcaster();
        await using var provider = BuildMediatRContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new MarkProductUnavailableCommand(productId, "Acabou o insumo", AutoRestoreNextDay: true));
        var second = await sender.Send(new MarkProductUnavailableCommand(productId, "Praça fechada", AutoRestoreNextDay: true));

        second.IsSuccess.Should().BeTrue();
        second.Value!.UnavailableReason.Should().Be("Praça fechada");
        broadcaster.UnavailableCalls.Should().HaveCount(2);
    }

    /// <summary>Cenário Gherkin "Retorno à disponibilidade, manual" (US-015 §3.1).</summary>
    [Fact]
    public async Task Marcar_Produto_Disponivel_Reverte_A_Indisponibilidade_E_Propaga()
    {
        var tenantId = await SeedTenantAsync();
        var (_, productId) = await SeedCategoryAndProductAsync(tenantId, "Pizza Portuguesa");
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingAvailabilityBroadcaster();
        await using var provider = BuildMediatRContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new MarkProductUnavailableCommand(productId, "Acabou o insumo", AutoRestoreNextDay: true));
        var result = await sender.Send(new MarkProductAvailableCommand(productId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAvailable.Should().BeTrue();
        result.Value!.UnavailableReason.Should().BeNull();

        var persisted = await db.Products.AsNoTracking().SingleAsync(p => p.Id == productId);
        persisted.IsAvailable.Should().BeTrue();
        persisted.UnavailableSince.Should().BeNull();

        broadcaster.AvailableCalls.Should().ContainSingle(call => call.ProductId == productId);
    }

    /// <summary>"Lista de itens indisponíveis sempre visível ao garçom/gestor" (US-015 §10).</summary>
    [Fact]
    public async Task Listar_Indisponiveis_So_Retorna_Produtos_Marcados_Indisponiveis()
    {
        var tenantId = await SeedTenantAsync();
        var (_, unavailableProductId) = await SeedCategoryAndProductAsync(tenantId, "Pizza Calabresa");
        var (_, availableProductId) = await SeedCategoryAndProductAsync(tenantId, "Pizza Mussarela");
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingAvailabilityBroadcaster();
        await using var provider = BuildMediatRContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new MarkProductUnavailableCommand(unavailableProductId, "Acabou o insumo", AutoRestoreNextDay: true));

        var listResult = await sender.Send(new ListUnavailableProductsQuery());

        listResult.IsSuccess.Should().BeTrue();
        var item = listResult.Value!.Items.Should().ContainSingle().Subject;
        item.ProductId.Should().Be(unavailableProductId);
        listResult.Value!.Items.Should().NotContain(i => i.ProductId == availableProductId);
    }

    /// <summary>Cenário Gherkin "Retorno automático no novo dia operacional" (US-015 §3.1/§4).</summary>
    [Fact]
    public async Task Restaurar_Produtos_Apos_Virada_Do_Dia_Operacional_Marca_Disponivel_De_Novo()
    {
        var tenantId = await SeedTenantAsync();
        var (_, oldProductId) = await SeedCategoryAndProductAsync(tenantId, "Pizza Calabresa");
        var (_, recentProductId) = await SeedCategoryAndProductAsync(tenantId, "Pizza Mussarela");
        var (_, manualProductId) = await SeedCategoryAndProductAsync(tenantId, "Pizza sazonal");
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var broadcaster = new RecordingAvailabilityBroadcaster();
        await using var provider = BuildMediatRContainer(db, tenantContext, broadcaster);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new MarkProductUnavailableCommand(oldProductId, "Acabou o insumo", AutoRestoreNextDay: true));
        await sender.Send(new MarkProductUnavailableCommand(recentProductId, "Acabou o insumo", AutoRestoreNextDay: true));
        await sender.Send(new MarkProductUnavailableCommand(manualProductId, "Fora de temporada", AutoRestoreNextDay: false));

        // Recua unavailable_since do primeiro produto para 2 dias atrás — simula que ficou
        // indisponível num dia operacional anterior ao atual (Domain não tem mutador público para
        // "voltar no tempo" de propósito; mesma limitação/técnica já documentada em
        // CatalogIntegrationTests.Cardapio_Publico_Resolve_Tenant_Por_Host... para o campo
        // Tenant.Domain).
        var twoDaysAgo = DateTimeOffset.UtcNow.AddDays(-2);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE product SET unavailable_since = {twoDaysAgo} WHERE id = {oldProductId}");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE product SET unavailable_since = {twoDaysAgo} WHERE id = {manualProductId}");

        // O UPDATE direto não sincroniza entidades já rastreadas. O worker real abre um escopo/
        // DbContext novo a cada execução; limpar aqui reproduz esse ciclo e força a consulta do
        // comando a ler unavailable_since atualizado no PostgreSQL.
        db.ChangeTracker.Clear();

        var restoreResult = await sender.Send(new RestoreProductsPastBusinessDayCommand(tenantId));

        restoreResult.IsSuccess.Should().BeTrue();
        restoreResult.Value.Should().Be(1, "só o produto marcado indisponível num dia operacional anterior deve voltar");

        var oldProduct = await db.Products.AsNoTracking().SingleAsync(p => p.Id == oldProductId);
        oldProduct.IsAvailable.Should().BeTrue("passou da virada do dia operacional — retorno automático (US-015 §3.1)");

        var recentProduct = await db.Products.AsNoTracking().SingleAsync(p => p.Id == recentProductId);
        recentProduct.IsAvailable.Should().BeFalse("ainda está dentro do mesmo dia operacional em que foi marcado — não volta sozinho");

        var manualProduct = await db.Products.AsNoTracking().SingleAsync(p => p.Id == manualProductId);
        manualProduct.IsAvailable.Should().BeFalse("autoRestoreNextDay=false exige retorno manual, mesmo depois da virada");
        manualProduct.AutoRestoreNextDay.Should().BeFalse();

        broadcaster.AvailableCalls.Should().ContainSingle(call => call.ProductId == oldProductId);
    }

    /// <summary>Isolamento multi-tenant (RLS, ADR-004) — produto indisponível de um tenant não aparece para outro.</summary>
    [Fact]
    public async Task Lista_De_Indisponiveis_Nao_Vaza_Entre_Tenants()
    {
        var tenantA = await SeedTenantAsync();
        var tenantB = await SeedTenantAsync();
        var (_, productA) = await SeedCategoryAndProductAsync(tenantA, "Pizza Calabresa");

        var contextA = new StaticTenantContext(tenantA, userId: Guid.NewGuid());
        await using (var dbA = _fixture.CreateAppDbContext(contextA))
        await using (var providerA = BuildMediatRContainer(dbA, contextA, new RecordingAvailabilityBroadcaster()))
        {
            await providerA.GetRequiredService<ISender>().Send(
                new MarkProductUnavailableCommand(productA, "Acabou o insumo", AutoRestoreNextDay: true));
        }

        var contextB = new StaticTenantContext(tenantB, userId: Guid.NewGuid());
        await using var dbB = _fixture.CreateAppDbContext(contextB);
        await using var providerB = BuildMediatRContainer(dbB, contextB, new RecordingAvailabilityBroadcaster());

        var listResult = await providerB.GetRequiredService<ISender>().Send(new ListUnavailableProductsQuery());

        listResult.Value!.Items.Should().BeEmpty("o RLS (ADR-004) impede que o produto indisponível do tenant A apareça para o tenant B");
    }

    private async Task<Guid> SeedTenantAsync()
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        return tenantId;
    }

    private async Task<(Guid CategoryId, Guid ProductId)> SeedCategoryAndProductAsync(Guid tenantId, string productName)
    {
        var tenantContext = new StaticTenantContext(tenantId);
        await using var db = _fixture.CreateAppDbContext(tenantContext);

        var category = Category.Create(tenantId, "Pizzas Salgadas");
        db.Categories.Add(category);

        var product = Product.Create(tenantId, category.Id, productName);
        db.Products.Add(product);

        await db.SaveChangesAsync();

        return (category.Id, product.Id);
    }

    private static ServiceProvider BuildMediatRContainer(
        IApplicationDbContext db, ICurrentTenantContext tenantContext, IAvailabilityBroadcaster broadcaster)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton(tenantContext);
        services.AddSingleton<IEventOriginProvider, EdgeEventOriginProvider>();
        services.AddSingleton(broadcaster);
        services.AddSingleton<Nexora.Application.Abstractions.Realtime.IAlertsBroadcaster, RecordingAlertsBroadcaster>();
        services.AddScoped<IAlertRaiser, AlertRaiser>();

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
