using Nexora.Application.Catalog.FractionPricing.Queries.PreviewFractionPricing;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-013 (Pizza meio a meio com frações) contra um PostgreSQL real (Testcontainers, mesma
/// <see cref="PostgresFixture"/> das demais suites). Dois focos, conforme decisão de escopo do
/// relatório da tarefa: (1) a constraint de banco (função + <c>CONSTRAINT TRIGGER ...
/// DEFERRABLE INITIALLY DEFERRED</c> da migration <c>AddOrderItemFractionWeightConstraint</c>)
/// realmente recusa uma soma de pesos que não feche em 1,0 — testada manipulando
/// <c>Order</c>/<c>OrderItem</c>/<c>OrderItemFraction</c> DIRETAMENTE via <c>IApplicationDbContext</c>,
/// sem nenhum Command/Controller de pedido (não existe — ver decisão de escopo); e (2) o pipeline
/// MediatR completo de <c>PreviewFractionPricingQuery</c> (carga de variante/produto/preço real do
/// banco + resolução da regra do tenant), que os testes unitários de
/// <c>FractionPricingCalculatorTests</c> não alcançam por serem puros.
/// </summary>
[Collection("Postgres")]
public sealed class FractionPricingIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public FractionPricingIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    // -----------------------------------------------------------------------------------------
    // Constraint de banco (US-013 §8: "A soma de weight das frações de um item deve ser
    // exatamente 1,0 — garantido por constraint de banco, não por validação de aplicação").
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Constraint_Aceita_Soma_De_Pesos_Exatamente_Igual_A_Um()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        var (mussarelaId, calabresaId) = await SeedTwoCompatibleFlavorsAsync(tenantContext, tenantId);

        await using var db = _fixture.CreateAppDbContext(tenantContext);

        var order = Order.Create(tenantId, Guid.NewGuid(), Channel.DineIn, "A01", DateOnly.FromDateTime(DateTime.UtcNow));
        db.Orders.Add(order);

        var item = OrderItem.Create(tenantId, order.Id, mussarelaId, unitPrice: 52.00m);
        db.OrderItems.Add(item);
        db.OrderItemFractions.Add(OrderItemFraction.Create(tenantId, item.Id, mussarelaId, 0.5m, 45.00m, 0));
        db.OrderItemFractions.Add(OrderItemFraction.Create(tenantId, item.Id, calabresaId, 0.5m, 52.00m, 1));

        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync("0.5 + 0.5 = 1.0 exato — a constraint deve aceitar");
    }

    [Fact]
    public async Task Constraint_Recusa_Soma_De_Pesos_Que_Nao_Fecha_Em_Um()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        var (mussarelaId, calabresaId) = await SeedTwoCompatibleFlavorsAsync(tenantContext, tenantId);

        await using var db = _fixture.CreateAppDbContext(tenantContext);

        var order = Order.Create(tenantId, Guid.NewGuid(), Channel.DineIn, "A02", DateOnly.FromDateTime(DateTime.UtcNow));
        db.Orders.Add(order);

        var item = OrderItem.Create(tenantId, order.Id, mussarelaId, unitPrice: 52.00m);
        db.OrderItems.Add(item);
        // 0.5 + 0.4 = 0.9 — não fecha em 1,0.
        db.OrderItemFractions.Add(OrderItemFraction.Create(tenantId, item.Id, mussarelaId, 0.5m, 45.00m, 0));
        db.OrderItemFractions.Add(OrderItemFraction.Create(tenantId, item.Id, calabresaId, 0.4m, 52.00m, 1));

        var act = async () => await db.SaveChangesAsync();

        var assertion = await act.Should().ThrowAsync<PostgresException>(
            "a função + CONSTRAINT TRIGGER de AddOrderItemFractionWeightConstraint deve recusar no COMMIT (deferido)");
        assertion.Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        assertion.Which.Message.Should().Contain(
            "soma dos pesos",
            "a mensagem lançada por check_order_item_fraction_weight_sum deve chegar até a exceção do PostgreSQL");
    }

    [Fact]
    public async Task Constraint_Recusa_Delete_Que_Deixa_A_Soma_Restante_Invalida()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        var (mussarelaId, calabresaId) = await SeedTwoCompatibleFlavorsAsync(tenantContext, tenantId);

        Guid itemId;
        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext))
        {
            var order = Order.Create(tenantId, Guid.NewGuid(), Channel.DineIn, "A03", DateOnly.FromDateTime(DateTime.UtcNow));
            seedDb.Orders.Add(order);
            var item = OrderItem.Create(tenantId, order.Id, mussarelaId, unitPrice: 52.00m);
            itemId = item.Id;
            seedDb.OrderItems.Add(item);
            seedDb.OrderItemFractions.Add(OrderItemFraction.Create(tenantId, item.Id, mussarelaId, 0.5m, 45.00m, 0));
            seedDb.OrderItemFractions.Add(OrderItemFraction.Create(tenantId, item.Id, calabresaId, 0.5m, 52.00m, 1));
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        var fractions = await db.OrderItemFractions.Where(f => f.OrderItemId == itemId).ToListAsync();
        // Apaga só uma das duas frações — a soma restante (0.5) não fecha em 1,0, e a constraint
        // (também disparada por DELETE) deve recusar a transação inteira.
        db.OrderItemFractions.Remove(fractions[0]);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>("apagar uma fração e deixar a soma em 0.5 também viola a constraint");
    }

    // -----------------------------------------------------------------------------------------
    // Pipeline completo de PreviewFractionPricingQuery (US-013 §4/§7).
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Preview_Aplica_Highest_Por_Padrao_Quando_Tenant_Nao_Configurou_Regra()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        var (mussarelaId, calabresaId) = await SeedTwoCompatibleFlavorsAsync(tenantContext, tenantId, "Mussarela", "Calabresa");
        await SeedPriceAsync(tenantContext, tenantId, mussarelaId, 45.00m);
        await SeedPriceAsync(tenantContext, tenantId, calabresaId, 52.00m);

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var query = new PreviewFractionPricingQuery(
            new[]
            {
                new FractionSelectionRequest(mussarelaId, 0.5m),
                new FractionSelectionRequest(calabresaId, 0.5m),
            },
            Channel: null);

        var result = await sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitPrice.Should().Be(52.00m, "cenário Gherkin 'Precificação por maior valor' — padrão HIGHEST sem config do tenant");
        result.Value.PriceRule.Should().Be("HIGHEST");
        result.Value.Description.Should().Be("G · Mussarela / Calabresa");
        result.Value.Fractions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Preview_Usa_A_Regra_Configurada_No_TenantConfig()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        var (mussarelaId, calabresaId) = await SeedTwoCompatibleFlavorsAsync(tenantContext, tenantId, "Mussarela", "Calabresa");
        await SeedPriceAsync(tenantContext, tenantId, mussarelaId, 45.00m);
        await SeedPriceAsync(tenantContext, tenantId, calabresaId, 52.00m);

        await using (var configDb = _fixture.CreateAppDbContext(tenantContext))
        {
            var config = TenantConfig.Create(tenantId);
            config.UpdateOperation("""{"halfAndHalfPricing":"AVERAGE"}""");
            configDb.TenantConfigs.Add(config);
            await configDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var query = new PreviewFractionPricingQuery(
            new[]
            {
                new FractionSelectionRequest(mussarelaId, 0.5m),
                new FractionSelectionRequest(calabresaId, 0.5m),
            },
            Channel: null);

        var result = await sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitPrice.Should().Be(48.50m, "cenário Gherkin 'Precificação por média' — (45.00 + 52.00) / 2");
        result.Value.PriceRule.Should().Be("AVERAGE");
    }

    [Fact]
    public async Task Preview_Recusa_Grupos_De_Fracao_Divergentes()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        Guid pizzaVariantId, burgerVariantId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        {
            var category = Category.Create(tenantId, "Cardápio");
            db.Categories.Add(category);

            var pizza = Product.Create(tenantId, category.Id, "Mussarela", allowsFractions: true, maxFractions: 4);
            pizza.SetFractionGroup("PIZZA");
            db.Products.Add(pizza);
            var pizzaVariant = ProductVariant.Create(tenantId, pizza.Id, "Mussarela G", sizeCode: "G");
            db.ProductVariants.Add(pizzaVariant);
            pizzaVariantId = pizzaVariant.Id;

            var burger = Product.Create(tenantId, category.Id, "X-Salada", allowsFractions: true, maxFractions: 2);
            burger.SetFractionGroup("HAMBURGUER");
            db.Products.Add(burger);
            var burgerVariant = ProductVariant.Create(tenantId, burger.Id, "X-Salada G", sizeCode: "G");
            db.ProductVariants.Add(burgerVariant);
            burgerVariantId = burgerVariant.Id;

            db.Prices.Add(Price.Create(tenantId, pizzaVariant.Id, Channel.DineIn, 45.00m));
            db.Prices.Add(Price.Create(tenantId, burgerVariant.Id, Channel.DineIn, 30.00m));
            await db.SaveChangesAsync();
        }

        await using var queryDb = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(queryDb, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var query = new PreviewFractionPricingQuery(
            new[]
            {
                new FractionSelectionRequest(pizzaVariantId, 0.5m),
                new FractionSelectionRequest(burgerVariantId, 0.5m),
            },
            Channel: null);

        var result = await sender.Send(query);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionGroupMismatch, "cenário Gherkin 'Grupos de fração distintos' (US-013 §4)");
    }

    [Fact]
    public async Task Preview_Recusa_Tamanhos_Incompativeis()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        Guid mussarelaGId, calabresaMId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        {
            var category = Category.Create(tenantId, "Pizzas");
            db.Categories.Add(category);

            var mussarela = Product.Create(tenantId, category.Id, "Mussarela", allowsFractions: true, maxFractions: 4);
            mussarela.SetFractionGroup("PIZZA");
            db.Products.Add(mussarela);
            var mussarelaG = ProductVariant.Create(tenantId, mussarela.Id, "Mussarela G", sizeCode: "G");
            db.ProductVariants.Add(mussarelaG);
            mussarelaGId = mussarelaG.Id;

            var calabresa = Product.Create(tenantId, category.Id, "Calabresa", allowsFractions: true, maxFractions: 4);
            calabresa.SetFractionGroup("PIZZA");
            db.Products.Add(calabresa);
            var calabresaM = ProductVariant.Create(tenantId, calabresa.Id, "Calabresa M", sizeCode: "M");
            db.ProductVariants.Add(calabresaM);
            calabresaMId = calabresaM.Id;

            db.Prices.Add(Price.Create(tenantId, mussarelaG.Id, Channel.DineIn, 45.00m));
            db.Prices.Add(Price.Create(tenantId, calabresaM.Id, Channel.DineIn, 40.00m));
            await db.SaveChangesAsync();
        }

        await using var queryDb = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(queryDb, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var query = new PreviewFractionPricingQuery(
            new[]
            {
                new FractionSelectionRequest(mussarelaGId, 0.5m),
                new FractionSelectionRequest(calabresaMId, 0.5m),
            },
            Channel: null);

        var result = await sender.Send(query);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionSizeMismatch, "cenário Gherkin 'Tamanhos incompatíveis' (US-013 §4)");
    }

    [Fact]
    public async Task Preview_Recusa_Produto_Que_Nao_Permite_Fracionamento()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        Guid mussarelaGId, refrigeranteId;
        await using (var db = _fixture.CreateAppDbContext(tenantContext))
        {
            var category = Category.Create(tenantId, "Cardápio");
            db.Categories.Add(category);

            var mussarela = Product.Create(tenantId, category.Id, "Mussarela", allowsFractions: true, maxFractions: 4);
            mussarela.SetFractionGroup("PIZZA");
            db.Products.Add(mussarela);
            var mussarelaG = ProductVariant.Create(tenantId, mussarela.Id, "Mussarela G", sizeCode: "G");
            db.ProductVariants.Add(mussarelaG);
            mussarelaGId = mussarelaG.Id;

            // Produto SEM AllowsFractions — não deveria poder entrar num meio a meio.
            var refrigerante = Product.Create(tenantId, category.Id, "Refrigerante Lata");
            db.Products.Add(refrigerante);
            var refrigeranteVariant = ProductVariant.Create(tenantId, refrigerante.Id, "Refrigerante Lata Único", sizeCode: "G");
            db.ProductVariants.Add(refrigeranteVariant);
            refrigeranteId = refrigeranteVariant.Id;

            db.Prices.Add(Price.Create(tenantId, mussarelaG.Id, Channel.DineIn, 45.00m));
            db.Prices.Add(Price.Create(tenantId, refrigeranteVariant.Id, Channel.DineIn, 8.00m));
            await db.SaveChangesAsync();
        }

        await using var queryDb = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(queryDb, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var query = new PreviewFractionPricingQuery(
            new[]
            {
                new FractionSelectionRequest(mussarelaGId, 0.5m),
                new FractionSelectionRequest(refrigeranteId, 0.5m),
            },
            Channel: null);

        var result = await sender.Send(query);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionNotAllowed);
    }

    [Fact]
    public async Task Preview_Recusa_Produto_Indisponivel()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        var (mussarelaId, calabresaId) = await SeedTwoCompatibleFlavorsAsync(tenantContext, tenantId);
        await SeedPriceAsync(tenantContext, tenantId, mussarelaId, 45.00m);
        await SeedPriceAsync(tenantContext, tenantId, calabresaId, 52.00m);

        await using (var setupDb = _fixture.CreateAppDbContext(tenantContext))
        {
            var product = await setupDb.ProductVariants
                .Where(variant => variant.Id == calabresaId)
                .Select(variant => variant.Product)
                .SingleAsync();
            product.MarkUnavailable("Sem estoque");
            await setupDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        var query = new PreviewFractionPricingQuery(
            new[]
            {
                new FractionSelectionRequest(mussarelaId, 0.5m),
                new FractionSelectionRequest(calabresaId, 0.5m),
            },
            Channel: null);

        var result = await sender.Send(query);

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.FractionVariantNotFound);
    }

    // -----------------------------------------------------------------------------------------
    // Seeds compartilhados.
    // -----------------------------------------------------------------------------------------

    private async Task<Guid> SeedTenantAsync()
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        return tenantId;
    }

    /// <summary>Duas variantes de tamanho "G", mesmo <c>FractionGroup</c> ("PIZZA"), prontas para compor um meio a meio válido.</summary>
    private async Task<(Guid MussarelaVariantId, Guid CalabresaVariantId)> SeedTwoCompatibleFlavorsAsync(
        StaticTenantContext tenantContext, Guid tenantId, string firstName = "Mussarela", string secondName = "Calabresa")
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext);

        var category = Category.Create(tenantId, "Pizzas Salgadas");
        db.Categories.Add(category);

        var first = Product.Create(tenantId, category.Id, firstName, allowsFractions: true, maxFractions: 4);
        first.SetFractionGroup("PIZZA");
        db.Products.Add(first);
        var firstVariant = ProductVariant.Create(tenantId, first.Id, $"{firstName} G", sizeCode: "G");
        db.ProductVariants.Add(firstVariant);

        var second = Product.Create(tenantId, category.Id, secondName, allowsFractions: true, maxFractions: 4);
        second.SetFractionGroup("PIZZA");
        db.Products.Add(second);
        var secondVariant = ProductVariant.Create(tenantId, second.Id, $"{secondName} G", sizeCode: "G");
        db.ProductVariants.Add(secondVariant);

        await db.SaveChangesAsync();

        return (firstVariant.Id, secondVariant.Id);
    }

    private async Task SeedPriceAsync(StaticTenantContext tenantContext, Guid tenantId, Guid variantId, decimal amount)
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        db.Prices.Add(Price.Create(tenantId, variantId, Channel.DineIn, amount));
        await db.SaveChangesAsync();
    }
}
