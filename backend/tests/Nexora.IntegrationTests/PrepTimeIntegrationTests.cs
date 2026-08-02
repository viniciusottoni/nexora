using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.PrepTime.Commands.ReassignProductStation;
using Nexora.Application.Catalog.PrepTime.Commands.UpdateVariantPrepTimeThresholds;
using Nexora.Application.Catalog.PrepTime.Queries.GetVariantPrepTimeAnalysis;
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
/// Cenários Gherkin da US-016 contra um PostgreSQL real (Testcontainers, mesma
/// <see cref="PostgresFixture"/> da US-001) e o pipeline MediatR real (Validation -&gt; Logging
/// -&gt; Transaction) — "Roteamento pela praça" (aqui, só a parte "stationId é reatribuível e
/// persiste", já que o roteamento de fila do KDS em si é da E-03/E-11, fora de escopo), "Limiar
/// herdado do tenant"/"Limiar específico do produto" e "Comparativo estimado versus real".
/// </summary>
[Collection("Postgres")]
public sealed class PrepTimeIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public PrepTimeIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpdatePrepTime_Com_Apenas_PrepMinutes_Persiste_E_Grava_Evento_Product_Updated()
    {
        var (tenantId, _, _, variantId) = await SeedCatalogAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new UpdateVariantPrepTimeThresholdsCommand(variantId, 14, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.PrepMinutes.Should().Be(14);

        var variant = await db.ProductVariants.SingleAsync(v => v.Id == variantId);
        variant.PrepMinutes.Should().Be(14);

        var domainEvent = await db.DomainEvents.SingleAsync(e => e.TenantId == tenantId && e.Type == "product.updated" && e.AggregateId == variantId);
        domainEvent.AggregateType.Should().Be("product_variant");
    }

    [Fact]
    public async Task UpdatePrepTime_Com_Limiares_Persiste_Os_Tres_Campos()
    {
        var (tenantId, _, _, variantId) = await SeedCatalogAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new UpdateVariantPrepTimeThresholdsCommand(variantId, 12, 15, 20));

        result.IsSuccess.Should().BeTrue();
        var variant = await db.ProductVariants.SingleAsync(v => v.Id == variantId);
        variant.WarnMinutes.Should().Be(15);
        variant.CriticalMinutes.Should().Be(20);
    }

    [Fact]
    public async Task UpdatePrepTime_Com_Critico_Menor_Que_Atencao_E_Recusado_Sem_Persistir()
    {
        var (tenantId, _, _, variantId) = await SeedCatalogAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new UpdateVariantPrepTimeThresholdsCommand(variantId, 10, 20, 15));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ValidationError);

        var variant = await db.ProductVariants.SingleAsync(v => v.Id == variantId);
        variant.PrepMinutes.Should().Be(10, "a variação foi criada com 10 minutos de preparo padrão e a atualização recusada não deve ter mudado nada");
    }

    /// <summary>Cenário Gherkin "Roteamento pela praça" (parte de catálogo — o roteamento de fila do KDS em si é da E-03/E-11).</summary>
    [Fact]
    public async Task ReassignStation_Atribui_A_Praca_Informada_E_Grava_Evento()
    {
        var (tenantId, storeId, productId, _) = await SeedCatalogAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var seedDb = _fixture.CreateAppDbContext(tenantContext);
        var forno = Station.Create(tenantId, storeId, "FORNO", "Forno");
        seedDb.Stations.Add(forno);
        await seedDb.SaveChangesAsync();

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ReassignProductStationCommand(productId, forno.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value!.StationCode.Should().Be("FORNO");

        var product = await db.Products.SingleAsync(p => p.Id == productId);
        product.StationId.Should().Be(forno.Id);

        var domainEvent = await db.DomainEvents.SingleAsync(e => e.TenantId == tenantId && e.Type == "product.updated" && e.AggregateId == productId);
        domainEvent.AggregateType.Should().Be("product");
    }

    [Fact]
    public async Task ReassignStation_Com_Praca_De_Outro_Tenant_E_Recusada_Com_404()
    {
        var (tenantId, _, productId, _) = await SeedCatalogAsync();
        var (otherTenantId, otherStoreId, _, _) = await SeedCatalogAsync();

        await using var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(otherTenantId));
        var otherStation = Station.Create(otherTenantId, otherStoreId, "CHAPA", "Chapa");
        seedDb.Stations.Add(otherStation);
        await seedDb.SaveChangesAsync();

        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ReassignProductStationCommand(productId, otherStation.Id));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.PrepTimeStationNotFound);
    }

    /// <summary>Cenário Gherkin "Limiar herdado do tenant": sem limiar próprio, herda o padrão provisionado (12/18 minutos).</summary>
    [Fact]
    public async Task GetPrepTimeAnalysis_Sem_Limiar_Proprio_Herda_O_Padrao_Do_Tenant()
    {
        var (tenantId, _, _, variantId) = await SeedCatalogAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetVariantPrepTimeAnalysisQuery(variantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.EffectiveWarnMinutes.Should().Be(12);
        result.Value!.WarnMinutesInherited.Should().BeTrue();
        result.Value!.EffectiveCriticalMinutes.Should().Be(18);
        result.Value!.CriticalMinutesInherited.Should().BeTrue();
    }

    /// <summary>Cenário Gherkin "Limiar herdado do tenant" com um padrão de tenant customizado (TenantConfig.Thresholds).</summary>
    [Fact]
    public async Task GetPrepTimeAnalysis_Usa_O_Padrao_Customizado_Do_Tenant_Quando_Configurado()
    {
        var (tenantId, _, _, variantId) = await SeedCatalogAsync();

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var config = await seedDb.TenantConfigs.SingleAsync(c => c.TenantId == tenantId);
            config.UpdateThresholds("""{"prepWarnMinutes":12,"prepCriticalMinutes":18}""");
            await seedDb.SaveChangesAsync();
        }

        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetVariantPrepTimeAnalysisQuery(variantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.EffectiveWarnMinutes.Should().Be(12);
        result.Value!.EffectiveCriticalMinutes.Should().Be(18);
    }

    /// <summary>Cenário Gherkin "Comparativo estimado versus real" — amostra suficiente (>= 20) e divergência acima de 20% sugerem ajuste.</summary>
    [Fact]
    public async Task GetPrepTimeAnalysis_Com_Amostra_Suficiente_E_Divergencia_Alta_Sugere_Ajuste()
    {
        var (tenantId, storeId, _, variantId) = await SeedCatalogAsync();

        // Preparo cadastrado é 10 min (default de ProductVariant.Create); real observado ~16 min
        // (divergência de 60%, bem acima do limiar de 20%) com amostra de 30 pedidos — acima do
        // mínimo de 20 (GetVariantPrepTimeAnalysisQueryHandler.MinimumSampleSize).
        await SeedDailyMetricAsync(tenantId, storeId, variantId, daysAgo: 1, quantity: 30, avgPrepSeconds: 960);

        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetVariantPrepTimeAnalysisQuery(variantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.SampleSize.Should().Be(30);
        result.Value!.ActualAvgMinutes.Should().Be(16.0m);
        result.Value!.Suggestion.Should().Be(16);
    }

    /// <summary>Amostra abaixo do mínimo não deve sugerir ajuste, mesmo com divergência alta.</summary>
    [Fact]
    public async Task GetPrepTimeAnalysis_Com_Amostra_Insuficiente_Nao_Sugere_Ajuste()
    {
        var (tenantId, storeId, _, variantId) = await SeedCatalogAsync();

        await SeedDailyMetricAsync(tenantId, storeId, variantId, daysAgo: 1, quantity: 3, avgPrepSeconds: 960);

        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetVariantPrepTimeAnalysisQuery(variantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.SampleSize.Should().Be(3);
        result.Value!.Suggestion.Should().BeNull();
        result.Value!.Note.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>Métricas fora da janela de 30 dias não entram na amostra.</summary>
    [Fact]
    public async Task GetPrepTimeAnalysis_Ignora_Metricas_Fora_Da_Janela_De_30_Dias()
    {
        var (tenantId, storeId, _, variantId) = await SeedCatalogAsync();

        await SeedDailyMetricAsync(tenantId, storeId, variantId, daysAgo: 45, quantity: 100, avgPrepSeconds: 1200);

        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetVariantPrepTimeAnalysisQuery(variantId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.SampleSize.Should().Be(0);
        result.Value!.ActualAvgMinutes.Should().BeNull();
    }

    private async Task<(Guid TenantId, Guid StoreId, Guid ProductId, Guid VariantId)> SeedCatalogAsync()
    {
        var tenantId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await db.SaveChangesAsync();
        }

        await using var tenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        tenantDb.TenantConfigs.Add(TenantConfig.Create(tenantId));
        tenantDb.Stores.Add(Store.Create(storeId, tenantId, "Loja de teste", isDefault: true));

        var category = Category.Create(tenantId, "Pizzas");
        tenantDb.Categories.Add(category);

        var product = Product.Create(tenantId, category.Id, "Pizza Mussarela");
        tenantDb.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, "Grande", prepMinutes: 10, isDefault: true);
        tenantDb.ProductVariants.Add(variant);

        await tenantDb.SaveChangesAsync();

        return (tenantId, storeId, product.Id, variant.Id);
    }

    /// <summary>
    /// Insere um dia de <c>metric_product_daily</c> via SQL bruto — <c>MetricProductDaily</c>
    /// (Domain) só expõe <c>Create</c> (todos os agregados zerados) sem nenhum mutador público
    /// hoje (a ingestão de métrica real é de uma US de BI/recálculo fora do escopo desta tarefa);
    /// inserir direto é a única forma de semear um cenário "com histórico" sem inventar um método
    /// de domínio que não pertence a esta US. Roda no mesmo <see cref="AppDbContext"/> que já tem
    /// <c>app.tenant_id</c> setado (<c>TenantConnectionInterceptor</c>) — sem isso, a política RLS
    /// de INSERT (<c>WITH CHECK tenant_id = current_tenant_id()</c>) recusaria a linha.
    /// </summary>
    private async Task SeedDailyMetricAsync(
        Guid tenantId, Guid storeId, Guid variantId, int daysAgo, int quantity, int avgPrepSeconds)
    {
        var businessDay = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-daysAgo);

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO metric_product_daily
                (tenant_id, store_id, variant_id, business_day, quantity, fraction_quantity, revenue, cost, margin, avg_prep_seconds, cancelled, refired, computed_at)
            VALUES
                ({tenantId}, {storeId}, {variantId}, {businessDay}, {quantity}, 0, 0, 0, 0, {avgPrepSeconds}, 0, 0, now())");
    }
}
