using Nexora.Application.Catalog.Prices.Commands.BulkAdjustPricesByCategory;
using Nexora.Application.Catalog.Prices.Commands.SetVariantChannelPrice;
using Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;
using Nexora.Contracts.Catalog;
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
/// Cenários Gherkin da US-014 (Preço por canal de venda) contra um PostgreSQL real (Testcontainers,
/// mesma <see cref="PostgresFixture"/> das demais suites) e o pipeline MediatR de produção
/// (Validation -&gt; Logging -&gt; Transaction). Arquivo NOVO (não edita
/// <c>CatalogIntegrationTests.cs</c>) para não colidir com o agente que trabalha em paralelo na
/// US-011/US-012/US-015/US-016.
/// </summary>
/// <remarks>
/// Este worktree ainda não tinha nenhuma camada de Application/Contracts/Api.Cloud para
/// categorias/produtos/variantes (US-010/US-011) no momento em que este teste foi escrito — só o
/// <c>Nexora.Domain</c> e a persistência (EF Core) já existiam. Por isso os testes semeiam
/// categoria/produto/variante/preço diretamente via as fábricas de domínio
/// (<see cref="Category.Create"/>/<see cref="Product.Create"/>/<see cref="ProductVariant.Create"/>/
/// <see cref="Price.Create"/>) em vez de via commands de outro módulo — mesmo padrão de
/// <c>SeedTenantAsync</c> já usado por <c>CatalogIntegrationTests</c> para semear o tenant.
/// </remarks>
[Collection("Postgres")]
public sealed class PricingIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public PricingIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin "Preço distinto no delivery" (US-014 §4) + "Herança do preço base" na mesma tabela.</summary>
    [Fact]
    public async Task Tabela_De_Precos_Traz_Canal_Proprio_E_Herda_DineIn_Nos_Demais()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var (categoryId, productId, variantId) = await SeedProductAsync(tenantContext, tenantId);

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext))
        {
            seedDb.Prices.Add(Price.Create(tenantId, variantId, Channel.DineIn, 45.00m));
            seedDb.Prices.Add(Price.Create(tenantId, variantId, Channel.Delivery, 52.00m));
            await seedDb.SaveChangesAsync();
        }

        var table = await sender.Send(new ListVariantPricesByChannelQuery(variantId));

        table.IsSuccess.Should().BeTrue();
        table.Value!.Channels.Should().HaveCount(4);
        table.Value!.Channels.Single(c => c.Channel == "DineIn").Amount.Should().Be(45.00m);
        table.Value!.Channels.Single(c => c.Channel == "DineIn").IsInherited.Should().BeFalse();
        table.Value!.Channels.Single(c => c.Channel == "Delivery").Amount.Should().Be(52.00m, "o cenário 'Preço distinto no delivery' exige o valor próprio do canal, não o do salão");
        table.Value!.Channels.Single(c => c.Channel == "Delivery").IsInherited.Should().BeFalse();
        table.Value!.Channels.Single(c => c.Channel == "Takeout").Amount.Should().Be(45.00m, "cenário 'Herança do preço base' — balcão sem preço próprio usa o preço do salão");
        table.Value!.Channels.Single(c => c.Channel == "Takeout").IsInherited.Should().BeTrue();
        table.Value!.Channels.Single(c => c.Channel == "Marketplace").IsInherited.Should().BeTrue();

        _ = categoryId;
        _ = productId;
    }

    /// <summary>Definir vários canais na mesma chamada historiza cada um independentemente e emite um price.changed por canal alterado.</summary>
    [Fact]
    public async Task Definir_Precos_De_Dois_Canais_Na_Mesma_Chamada_Historiza_Cada_Um()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var (_, _, variantId) = await SeedProductAsync(tenantContext, tenantId);

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext))
        {
            seedDb.Prices.Add(Price.Create(tenantId, variantId, Channel.DineIn, 45.00m));
            await seedDb.SaveChangesAsync();
        }

        var result = await sender.Send(new SetVariantChannelPriceCommand(
            variantId,
            new[]
            {
                new ChannelPriceEntry("DineIn", 46.00m),
                new ChannelPriceEntry("Delivery", 53.00m),
            }));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Channels.Single(c => c.Channel == "DineIn").Amount.Should().Be(46.00m);
        result.Value!.Channels.Single(c => c.Channel == "Delivery").Amount.Should().Be(53.00m);

        await using var assertDb = _fixture.CreateAppDbContext(tenantContext);
        var closedDineIn = await assertDb.Prices.AsNoTracking().SingleAsync(p => p.VariantId == variantId && p.Channel == Channel.DineIn && p.Amount == 45.00m);
        closedDineIn.ValidTo.Should().NotBeNull("o preço antigo do salão foi encerrado, não apagado");

        var priceChangedEvents = await assertDb.DomainEvents.Where(e => e.TenantId == tenantId && e.Type == "price.changed").ToListAsync();
        priceChangedEvents.Should().HaveCount(2, "os dois canais alterados na mesma chamada emitem um price.changed cada");
    }

    /// <summary>Canal repetido na mesma chamada é recusado.</summary>
    [Fact]
    public async Task Definir_Precos_Com_Canal_Repetido_E_Recusado()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var (_, _, variantId) = await SeedProductAsync(tenantContext, tenantId);

        var result = await sender.Send(new SetVariantChannelPriceCommand(
            variantId,
            new[]
            {
                new ChannelPriceEntry("DineIn", 46.00m),
                new ChannelPriceEntry("DineIn", 47.00m),
            }));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.PriceTableChannelDuplicated);
    }

    /// <summary>Cenário Gherkin "Reajuste em massa" (US-014 §4) — todos os preços da categoria no canal escolhido são atualizados e historizados, com AuditLog.</summary>
    [Fact]
    public async Task Reajuste_Em_Massa_Atualiza_Todas_As_Variacoes_Ativas_Da_Categoria_E_Grava_Audit_Log()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var (categoryId, _, firstVariantId) = await SeedProductAsync(tenantContext, tenantId, "Pizza Mussarela");
        var (_, _, secondVariantId) = await SeedProductAsync(tenantContext, tenantId, "Pizza Calabresa", categoryId);

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext))
        {
            seedDb.Prices.Add(Price.Create(tenantId, firstVariantId, Channel.Delivery, 50.00m));
            seedDb.Prices.Add(Price.Create(tenantId, secondVariantId, Channel.Delivery, 60.00m));
            await seedDb.SaveChangesAsync();
        }

        var result = await sender.Send(new BulkAdjustPricesByCategoryCommand(categoryId, "Delivery", 8m));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Updated.Should().Be(2, "as duas variações ativas da categoria devem ser atualizadas");

        await using var assertDb = _fixture.CreateAppDbContext(tenantContext);
        var firstNew = await assertDb.Prices.AsNoTracking().SingleAsync(p => p.VariantId == firstVariantId && p.Channel == Channel.Delivery && p.ValidTo == null);
        firstNew.Amount.Should().Be(54.00m, "50.00 + 8% = 54.00");
        var secondNew = await assertDb.Prices.AsNoTracking().SingleAsync(p => p.VariantId == secondVariantId && p.Channel == Channel.Delivery && p.ValidTo == null);
        secondNew.Amount.Should().Be(64.80m, "60.00 + 8% = 64.80");

        var oldFirst = await assertDb.Prices.AsNoTracking().SingleAsync(p => p.VariantId == firstVariantId && p.Channel == Channel.Delivery && p.Amount == 50.00m);
        oldFirst.ValidTo.Should().NotBeNull("o preço anterior fica historizado, nunca apagado (US-014 §4)");

        var auditLogs = await assertDb.AuditLogs.Where(a => a.TenantId == tenantId && a.Action == "PRICE_BULK_ADJUSTED").ToListAsync();
        auditLogs.Should().ContainSingle("o reajuste em massa deve constar em audit_log (US-014 §4/§8)");
    }

    /// <summary>Reajuste que resultaria em preço negativo é recusado por inteiro — nenhum preço é alterado (transacional).</summary>
    [Fact]
    public async Task Reajuste_Que_Resultaria_Em_Preco_Negativo_E_Recusado_Sem_Alterar_Nada()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var (categoryId, _, variantId) = await SeedProductAsync(tenantContext, tenantId);

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext))
        {
            seedDb.Prices.Add(Price.Create(tenantId, variantId, Channel.DineIn, 10.00m));
            await seedDb.SaveChangesAsync();
        }

        var result = await sender.Send(new BulkAdjustPricesByCategoryCommand(categoryId, "DineIn", -150m));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(
            ApiErrorCodes.ValidationError,
            "percentuais abaixo de -100% são recusados pelo ValidationBehavior antes de chegar ao handler");

        await using var assertDb = _fixture.CreateAppDbContext(tenantContext);
        var stillOpen = await assertDb.Prices.AsNoTracking().SingleAsync(p => p.VariantId == variantId && p.ValidTo == null);
        stillOpen.Amount.Should().Be(10.00m, "reajuste recusado não deve alterar preço nenhum, nem parcialmente");
    }

    /// <summary>Pedido histórico preserva o preço da época — preço antigo permanece consultável mesmo depois de reajustes futuros (US-014 §4).</summary>
    [Fact]
    public async Task Preco_Historico_Continua_Consultavel_Apos_Reajuste_Posterior()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var (_, _, variantId) = await SeedProductAsync(tenantContext, tenantId);

        Price historical;
        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext))
        {
            historical = Price.Create(tenantId, variantId, Channel.DineIn, 45.00m, validFrom: DateTimeOffset.UtcNow.AddDays(-30));
            seedDb.Prices.Add(historical);
            await seedDb.SaveChangesAsync();
        }

        var setResult = await sender.Send(new SetVariantChannelPriceCommand(variantId, new[] { new ChannelPriceEntry("DineIn", 52.00m) }));
        setResult.IsSuccess.Should().BeTrue();

        await using var assertDb = _fixture.CreateAppDbContext(tenantContext);
        var stillThere = await assertDb.Prices.AsNoTracking().SingleAsync(p => p.Id == historical.Id);
        stillThere.Amount.Should().Be(45.00m, "o preço da época de um pedido fechado há 30 dias continua acessível, mesmo com o preço atual em 52.00 (US-014, cenário 'Preço da época preservado')");
        stillThere.ValidTo.Should().NotBeNull();
    }

    private async Task<Guid> SeedTenantAsync()
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        return tenantId;
    }

    private async Task<(Guid CategoryId, Guid ProductId, Guid VariantId)> SeedProductAsync(
        StaticTenantContext tenantContext, Guid tenantId, string productName = "Pizza Mussarela", Guid? existingCategoryId = null)
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext);

        Guid categoryId;
        if (existingCategoryId is null)
        {
            var category = Category.Create(tenantId, "Pizzas Salgadas");
            db.Categories.Add(category);
            categoryId = category.Id;
        }
        else
        {
            categoryId = existingCategoryId.Value;
        }

        var product = Product.Create(tenantId, categoryId, productName);
        db.Products.Add(product);

        var variant = ProductVariant.Create(tenantId, product.Id, productName, isDefault: true);
        db.ProductVariants.Add(variant);

        await db.SaveChangesAsync();

        return (categoryId, product.Id, variant.Id);
    }
}
