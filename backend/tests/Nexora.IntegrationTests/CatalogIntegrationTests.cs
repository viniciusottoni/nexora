using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Catalog.Categories.Commands.CreateCategory;
using Nexora.Application.Catalog.Categories.Commands.DeactivateCategory;
using Nexora.Application.Catalog.Categories.Commands.ReorderCategories;
using Nexora.Application.Catalog.Categories.Queries.ListCategories;
using Nexora.Application.Catalog.Prices.Commands.SetVariantPrice;
using Nexora.Application.Catalog.Products.Commands.ActivateProduct;
using Nexora.Application.Catalog.Products.Commands.CreateProduct;
using Nexora.Application.Catalog.Products.Commands.DeactivateProduct;
using Nexora.Application.Catalog.Products.Commands.ReorderProducts;
using Nexora.Application.Catalog.Products.Commands.UpdateProduct;
using Nexora.Application.Catalog.Products.Queries.GetPublicMenu;
using Nexora.Application.Catalog.Products.Queries.ListProducts;
using Nexora.Application.Catalog.Variants.Commands.ActivateVariant;
using Nexora.Application.Catalog.Variants.Commands.CreateVariant;
using Nexora.Application.Catalog.Variants.Commands.DeactivateVariant;
using Nexora.Application.Catalog.Variants.Commands.MarkVariantAsDefault;
using Nexora.Application.Catalog.Variants.Commands.UpdateVariant;
using Nexora.Application.Catalog.Variants.Queries.ListVariantsForProduct;
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
/// Cenários Gherkin da US-010 (Cadastrar categorias e produtos) contra um PostgreSQL real
/// (Testcontainers, mesma <see cref="PostgresFixture"/> das demais suites) e o pipeline MediatR de
/// produção (Validation -&gt; Logging -&gt; Transaction) — mesmo padrão de
/// <c>StationsIntegrationTests</c>/<c>DevicesIntegrationTests</c>.
/// </summary>
[Collection("Postgres")]
public sealed class CatalogIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public CatalogIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Fluxo principal da US: criar categoria -&gt; criar produto vinculado -&gt; reordenar -&gt;
    /// desativar -&gt; o registro permanece (soft delete/IsActive=false, nunca exclusão física).
    /// </summary>
    [Fact]
    public async Task Criar_Categoria_Criar_Produto_Reordenar_E_Desativar_Preserva_O_Registro()
    {
        var tenantId = await SeedTenantAsync();
        var managerId = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(tenantId, userId: managerId);

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var categoryResult = await sender.Send(new CreateCategoryCommand("Pizzas Salgadas", "Sabores tradicionais", Position: 0));
        categoryResult.IsSuccess.Should().BeTrue();
        var categoryId = categoryResult.Value!.Id;

        var productResult = await sender.Send(new CreateProductCommand(
            categoryId,
            "Pizza Mussarela",
            StationId: null,
            Description: "Descrição da pizza",
            IngredientsText: "molho, mussarela, orégano",
            Allergens: new[] { "glúten", "lactose" },
            AllowsFractions: false,
            MaxFractions: 1,
            Position: 0,
            IsActive: true));

        productResult.IsSuccess.Should().BeTrue();
        productResult.Value!.IsActive.Should().BeTrue();
        productResult.Value!.CategoryName.Should().Be("Pizzas Salgadas");
        var productId = productResult.Value!.Id;

        // EVT-050 product.created foi emitido na mesma transação da criação (ADR-006).
        var createdEvent = await db.DomainEvents.SingleAsync(e => e.TenantId == tenantId && e.Type == "product.created");
        createdEvent.AggregateId.Should().Be(productId);

        var secondProduct = await sender.Send(new CreateProductCommand(
            categoryId, "Pizza Calabresa", null, null, null, null, false, 1, Position: 1, IsActive: true));
        secondProduct.IsSuccess.Should().BeTrue();
        var secondProductId = secondProduct.Value!.Id;

        // Cenário "Ordenação do cardápio" (US-010 §4): reordenar inverte a posição dos dois produtos.
        var reorderResult = await sender.Send(new ReorderProductsCommand(categoryId, new[] { secondProductId, productId }));
        reorderResult.IsSuccess.Should().BeTrue();

        var reordered = await db.Products.Where(p => p.CategoryId == categoryId).ToListAsync();
        reordered.Single(p => p.Id == secondProductId).SortOrder.Should().Be((short)0);
        reordered.Single(p => p.Id == productId).SortOrder.Should().Be((short)1);

        // Cenário "Desativação de produto" (US-010 §4): produto continua existindo, só sai de IsActive.
        var deactivateResult = await sender.Send(new DeactivateProductCommand(productId));
        deactivateResult.IsSuccess.Should().BeTrue();
        deactivateResult.Value!.IsActive.Should().BeFalse();

        var deactivatedProduct = await db.Products.SingleAsync(p => p.Id == productId);
        deactivatedProduct.IsActive.Should().BeFalse();
        deactivatedProduct.DeletedAt.Should().BeNull("desativação nunca apaga o registro — pedidos históricos continuam podendo referenciá-lo");
        deactivatedProduct.Name.Should().Be("Pizza Mussarela", "o cadastro precisa continuar legível mesmo desativado");

        // Reativação (US-010 §3.1, "distinto de indisponibilidade operacional") devolve o produto aos canais.
        var activateResult = await sender.Send(new ActivateProductCommand(productId));
        activateResult.IsSuccess.Should().BeTrue();
        activateResult.Value!.IsActive.Should().BeTrue();
    }

    /// <summary>Cenário Gherkin "Desativação de produto" (US-010 §4) explícito na categoria — desativar categoria preserva os produtos vinculados.</summary>
    [Fact]
    public async Task Desativar_Categoria_Preserva_Produtos_Vinculados()
    {
        var tenantId = await SeedTenantAsync();
        var managerId = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(tenantId, userId: managerId);

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var category = await sender.Send(new CreateCategoryCommand("Bebidas", null, 0));
        var product = await sender.Send(new CreateProductCommand(
            category.Value!.Id, "Refrigerante", null, null, null, null, false, 1, 0, true));

        var deactivateResult = await sender.Send(new DeactivateCategoryCommand(category.Value!.Id));
        deactivateResult.IsSuccess.Should().BeTrue();

        var categories = await sender.Send(new ListCategoriesQuery());
        categories.Value!.Items.Single(c => c.Id == category.Value!.Id).IsActive.Should().BeFalse();

        var persistedProduct = await db.Products.SingleAsync(p => p.Id == product.Value!.Id);
        persistedProduct.DeletedAt.Should().BeNull("desativar a categoria não apaga fisicamente os produtos vinculados");
        persistedProduct.IsActive.Should().BeTrue("desativar a categoria não desativa produtos em cascata — decisão documentada no relatório da tarefa");
    }

    /// <summary>Cenário Gherkin "Ordenação do cardápio" (US-010 §4) para categorias.</summary>
    [Fact]
    public async Task Reordenar_Categorias_Reflete_A_Nova_Posicao()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var pizzas = await sender.Send(new CreateCategoryCommand("Pizzas", null, 0));
        var bebidas = await sender.Send(new CreateCategoryCommand("Bebidas", null, 1));

        var reorderResult = await sender.Send(new ReorderCategoriesCommand(new[] { bebidas.Value!.Id, pizzas.Value!.Id }));
        reorderResult.IsSuccess.Should().BeTrue();

        var bebidasCategory = await db.Categories.SingleAsync(c => c.Id == bebidas.Value!.Id);
        var pizzasCategory = await db.Categories.SingleAsync(c => c.Id == pizzas.Value!.Id);
        bebidasCategory.SortOrder.Should().Be((short)0);
        pizzasCategory.SortOrder.Should().Be((short)1);
    }

    /// <summary>Reordenação recusada quando o conjunto de ids não bate com os produtos existentes na categoria (RN aplicada por <c>ReorderProductsCommandHandler</c>).</summary>
    [Fact]
    public async Task Reordenar_Produtos_Com_Conjunto_Divergente_E_Recusado()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var category = await sender.Send(new CreateCategoryCommand("Pizzas", null, 0));
        await sender.Send(new CreateProductCommand(category.Value!.Id, "Pizza Mussarela", null, null, null, null, false, 1, 0, true));

        var reorderResult = await sender.Send(new ReorderProductsCommand(category.Value!.Id, new[] { Guid.NewGuid() }));

        reorderResult.IsSuccess.Should().BeFalse();
        reorderResult.Code.Should().Be(ApiErrorCodes.CatalogReorderSetMismatch);
    }

    /// <summary>Criar produto com categoria de outro tenant é recusado — reforça o isolamento (US-010 §5, RN-015).</summary>
    [Fact]
    public async Task Criar_Produto_Com_Categoria_De_Outro_Tenant_E_Recusado()
    {
        var tenantA = await SeedTenantAsync();
        var tenantB = await SeedTenantAsync();

        var contextA = new StaticTenantContext(tenantA, userId: Guid.NewGuid());
        await using var dbA = _fixture.CreateAppDbContext(contextA);
        await using var providerA = MediatRTestContainerFactory.Build(dbA, contextA);
        var categoryA = await providerA.GetRequiredService<ISender>().Send(new CreateCategoryCommand("Pizzas", null, 0));

        var contextB = new StaticTenantContext(tenantB, userId: Guid.NewGuid());
        await using var dbB = _fixture.CreateAppDbContext(contextB);
        await using var providerB = MediatRTestContainerFactory.Build(dbB, contextB);

        var productResult = await providerB.GetRequiredService<ISender>().Send(new CreateProductCommand(
            categoryA.Value!.Id, "Pizza Mussarela", null, null, null, null, false, 1, 0, true));

        productResult.IsSuccess.Should().BeFalse();
        productResult.Code.Should().Be(ApiErrorCodes.ProductCategoryNotFound);
    }

    /// <summary>Isolamento multi-tenant (RLS, ADR-004) — DoD da US-010.</summary>
    [Fact]
    public async Task Listagem_De_Categorias_E_Produtos_Nao_Vaza_Entre_Tenants()
    {
        var tenantA = await SeedTenantAsync();
        var tenantB = await SeedTenantAsync();

        var contextA = new StaticTenantContext(tenantA, userId: Guid.NewGuid());
        await using (var dbA = _fixture.CreateAppDbContext(contextA))
        await using (var providerA = MediatRTestContainerFactory.Build(dbA, contextA))
        {
            var senderA = providerA.GetRequiredService<ISender>();
            var category = await senderA.Send(new CreateCategoryCommand("Pizzas", null, 0));
            await senderA.Send(new CreateProductCommand(category.Value!.Id, "Pizza Mussarela", null, null, null, null, false, 1, 0, true));
        }

        var contextB = new StaticTenantContext(tenantB, userId: Guid.NewGuid());
        await using var dbB = _fixture.CreateAppDbContext(contextB);
        await using var providerB = MediatRTestContainerFactory.Build(dbB, contextB);
        var senderB = providerB.GetRequiredService<ISender>();

        var categoriesB = await senderB.Send(new ListCategoriesQuery());
        var productsB = await senderB.Send(new ListProductsQuery(CategoryId: null));

        categoriesB.Value!.Items.Should().BeEmpty("o RLS (ADR-004) impede que a categoria do tenant A apareça na listagem do tenant B");
        productsB.Value!.Items.Should().BeEmpty("o RLS (ADR-004) impede que o produto do tenant A apareça na listagem do tenant B");
    }

    /// <summary>
    /// Cardápio público (<c>GetPublicMenuQuery</c>, US-010 §7) — resolve o tenant pelo domínio
    /// customizado, mesmo mecanismo de <c>GetPublicBrandingQuery</c>, e só devolve categorias e
    /// produtos ativos. Produto desativado (cenário "Desativação de produto") sai do cardápio
    /// público mas continua existindo no banco. Produto sem foto (cenário "Produto sem foto") tem
    /// <c>ImageUrl</c> nulo, nunca erro.
    /// </summary>
    [Fact]
    public async Task Cardapio_Publico_Resolve_Tenant_Por_Host_E_So_Devolve_Itens_Ativos()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        const string host = "pizzaria-dona-betinha.example.com";

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        // Domain não tem mutador público hoje (nenhuma US ainda expõe "configurar domínio
        // customizado") — atribuído diretamente via SQL só para o teste, mesma limitação já
        // registrada no relatório da tarefa.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE tenant SET domain = {host} WHERE id = {tenantId}");

        var category = await sender.Send(new CreateCategoryCommand("Pizzas Salgadas", null, 0));
        var visibleProduct = await sender.Send(new CreateProductCommand(
            category.Value!.Id, "Pizza Mussarela", null, "Descrição", "molho, mussarela", null, false, 1, 0, true));
        var hiddenProduct = await sender.Send(new CreateProductCommand(
            category.Value!.Id, "Pizza Descontinuada", null, null, null, null, false, 1, 1, true));
        await sender.Send(new DeactivateProductCommand(hiddenProduct.Value!.Id));
        var unavailableProduct = await sender.Send(new CreateProductCommand(
            category.Value!.Id, "Pizza sem insumo", null, null, null, null, false, 1, 2, true));
        var unavailableEntity = await db.Products.SingleAsync(p => p.Id == unavailableProduct.Value!.Id);
        unavailableEntity.MarkUnavailable("Ingrediente em falta");
        await db.SaveChangesAsync();

        var menuResult = await sender.Send(new GetPublicMenuQuery(host, Channel: "DINE_IN"));

        menuResult.IsSuccess.Should().BeTrue();
        menuResult.Value!.TenantId.Should().Be(tenantId);
        var menuCategory = menuResult.Value!.Categories.Should().ContainSingle().Subject;
        var product = menuCategory.Products.Should().ContainSingle().Subject;
        product.Id.Should().Be(visibleProduct.Value!.Id);
        product.ImageUrl.Should().BeNull("produto sem foto exibe marcador visual neutro no cliente, nunca erro — US-010 §4");
    }

    [Fact]
    public async Task Cardapio_Publico_Com_Host_Sem_Tenant_Correspondente_Retorna_Erro()
    {
        var tenantContext = new StaticTenantContext(tenantId: null);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);

        var result = await provider.GetRequiredService<ISender>().Send(new GetPublicMenuQuery("host-inexistente.example.com", null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.PublicMenuTenantNotFound);
    }

    /// <summary>Cenário Gherkin "Produto sem variação" (US-011 §4) — todo produto recém-criado já nasce com uma variação padrão implícita.</summary>
    [Fact]
    public async Task Criar_Produto_Cria_Variacao_Padrao_Implicita()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var category = await sender.Send(new CreateCategoryCommand("Bebidas", null, 0));
        var product = await sender.Send(new CreateProductCommand(
            category.Value!.Id, "Refrigerante Lata", null, null, null, null, false, 1, 0, true));

        var variants = await db.ProductVariants.Where(v => v.ProductId == product.Value!.Id).ToListAsync();

        var variant = variants.Should().ContainSingle("produto sem tamanhos cadastrados recebe uma única variação implícita").Subject;
        variant.IsDefault.Should().BeTrue();
        variant.Name.Should().Be("Refrigerante Lata");
        variant.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Cenário Gherkin "Produto com três tamanhos" (US-011 §4) — três variações com preços
    /// distintos e o cardápio público exibe "a partir de" com a menor delas.
    /// </summary>
    [Fact]
    public async Task Criar_Variacoes_Com_Precos_Distintos_Cardapio_Publico_Exibe_A_Partir_Do_Menor()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());
        const string host = "pizzaria-tres-tamanhos.example.com";

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE tenant SET domain = {host} WHERE id = {tenantId}");

        var category = await sender.Send(new CreateCategoryCommand("Pizzas Salgadas", null, 0));
        var product = await sender.Send(new CreateProductCommand(
            category.Value!.Id, "Pizza Mussarela", null, null, null, null, false, 1, 0, true));
        var productId = product.Value!.Id;

        // O produto já nasceu com uma variação implícita (US-011 §3.1) — aqui ela vira "Pequena" e
        // ganha as outras duas variações explícitas, cada uma com preço próprio.
        var implicitVariantId = (await db.ProductVariants.SingleAsync(v => v.ProductId == productId)).Id;
        await sender.Send(new UpdateVariantCommand(implicitVariantId, "Pequena", "P", null));
        var pequena = await sender.Send(new SetVariantPriceCommand(implicitVariantId, 35.00m, null));
        pequena.IsSuccess.Should().BeTrue();

        var media = await sender.Send(new CreateVariantCommand(productId, "Média", "M", null, null, false, 45.00m, null));
        media.IsSuccess.Should().BeTrue();

        var grande = await sender.Send(new CreateVariantCommand(productId, "Grande", "G", null, null, false, 52.00m, null));
        grande.IsSuccess.Should().BeTrue();

        var variantsList = await sender.Send(new ListVariantsForProductQuery(productId, null));
        variantsList.Value!.Items.Should().HaveCount(3, "as três variações continuam existindo e visíveis à gestão");

        var menuResult = await sender.Send(new GetPublicMenuQuery(host, Channel: null));
        menuResult.IsSuccess.Should().BeTrue();
        var menuProduct = menuResult.Value!.Categories.Single().Products.Should().ContainSingle().Subject;
        menuProduct.FromPrice.Should().Be(35.00m, "o cardápio exibe o preço da menor variação, com indicação 'a partir de' (US-011 §4)");
    }

    /// <summary>
    /// Cenário Gherkin "Alteração de preço registrada" (US-011 §4) — o preço anterior permanece
    /// historizado com <c>valid_from</c>/<c>valid_to</c> e o evento <c>price.changed</c> é emitido.
    /// </summary>
    [Fact]
    public async Task Alterar_Preco_Da_Variante_Historiza_O_Valor_Anterior_E_Emite_Price_Changed()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var category = await sender.Send(new CreateCategoryCommand("Pizzas", null, 0));
        var product = await sender.Send(new CreateProductCommand(category.Value!.Id, "Pizza Calabresa", null, null, null, null, false, 1, 0, true));
        var variantId = (await db.ProductVariants.SingleAsync(v => v.ProductId == product.Value!.Id)).Id;

        var firstPrice = await sender.Send(new SetVariantPriceCommand(variantId, 45.00m, null));
        firstPrice.IsSuccess.Should().BeTrue();
        var firstPriceId = firstPrice.Value!.Id;

        var secondPrice = await sender.Send(new SetVariantPriceCommand(variantId, 48.00m, null));
        secondPrice.IsSuccess.Should().BeTrue();
        secondPrice.Value!.Amount.Should().Be(48.00m);
        secondPrice.Value!.ValidTo.Should().BeNull("o novo preço fica vigente");

        var closedPrice = await db.Prices.AsNoTracking().SingleAsync(p => p.Id == firstPriceId);
        closedPrice.Amount.Should().Be(45.00m, "o valor antigo permanece historizado, nunca sobrescrito");
        closedPrice.ValidTo.Should().NotBeNull("o preço antigo foi encerrado, não apagado");

        var priceChangedEvent = await db.DomainEvents.SingleAsync(e => e.TenantId == tenantId && e.Type == "price.changed");
        priceChangedEvent.AggregateId.Should().Be(variantId);
    }

    /// <summary>
    /// Cenário Gherkin "Exclusão com histórico" (US-011 §4) — não existe endpoint de exclusão
    /// física de variante; desativar é a única operação de remoção e preserva o registro.
    /// </summary>
    [Fact]
    public async Task Desativar_Variante_Preserva_O_Registro_E_Reativar_Devolve_Ao_Cardapio()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var category = await sender.Send(new CreateCategoryCommand("Pizzas", null, 0));
        var product = await sender.Send(new CreateProductCommand(category.Value!.Id, "Pizza Frango", null, null, null, null, false, 1, 0, true));
        var variantId = (await db.ProductVariants.SingleAsync(v => v.ProductId == product.Value!.Id)).Id;

        var deactivateResult = await sender.Send(new DeactivateVariantCommand(variantId));
        deactivateResult.IsSuccess.Should().BeTrue();
        deactivateResult.Value!.IsActive.Should().BeFalse();

        var deactivatedVariant = await db.ProductVariants.SingleAsync(v => v.Id == variantId);
        deactivatedVariant.DeletedAt.Should().BeNull("desativação nunca é exclusão física — não existe endpoint de exclusão de variante nesta história");
        deactivatedVariant.Name.Should().Be("Pizza Frango");

        var activateResult = await sender.Send(new ActivateVariantCommand(variantId));
        activateResult.IsSuccess.Should().BeTrue();
        activateResult.Value!.IsActive.Should().BeTrue();
    }

    /// <summary>Só pode existir uma variação padrão por produto — marcar uma nova desmarca a anterior.</summary>
    [Fact]
    public async Task Marcar_Variante_Como_Padrao_Desmarca_A_Anterior()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var category = await sender.Send(new CreateCategoryCommand("Pizzas", null, 0));
        var product = await sender.Send(new CreateProductCommand(category.Value!.Id, "Pizza Marguerita", null, null, null, null, false, 1, 0, true));
        var productId = product.Value!.Id;
        var implicitVariantId = (await db.ProductVariants.SingleAsync(v => v.ProductId == productId)).Id;

        var grande = await sender.Send(new CreateVariantCommand(productId, "Grande", "G", null, null, false, 60.00m, null));
        grande.IsSuccess.Should().BeTrue();

        var markResult = await sender.Send(new MarkVariantAsDefaultCommand(grande.Value!.Id));
        markResult.IsSuccess.Should().BeTrue();
        markResult.Value!.IsDefault.Should().BeTrue();

        var previousDefault = await db.ProductVariants.SingleAsync(v => v.Id == implicitVariantId);
        previousDefault.IsDefault.Should().BeFalse("só pode existir uma variação padrão por produto (US-011 §3.1)");
    }

    /// <summary>Criar variante em produto de outro tenant é recusado — reforça o isolamento (RLS, ADR-004).</summary>
    [Fact]
    public async Task Criar_Variante_Em_Produto_De_Outro_Tenant_E_Recusado()
    {
        var tenantA = await SeedTenantAsync();
        var tenantB = await SeedTenantAsync();

        var contextA = new StaticTenantContext(tenantA, userId: Guid.NewGuid());
        await using var dbA = _fixture.CreateAppDbContext(contextA);
        await using var providerA = MediatRTestContainerFactory.Build(dbA, contextA);
        var categoryA = await providerA.GetRequiredService<ISender>().Send(new CreateCategoryCommand("Pizzas", null, 0));
        var productA = await providerA.GetRequiredService<ISender>().Send(new CreateProductCommand(
            categoryA.Value!.Id, "Pizza Mussarela", null, null, null, null, false, 1, 0, true));

        var contextB = new StaticTenantContext(tenantB, userId: Guid.NewGuid());
        await using var dbB = _fixture.CreateAppDbContext(contextB);
        await using var providerB = MediatRTestContainerFactory.Build(dbB, contextB);

        var variantResult = await providerB.GetRequiredService<ISender>().Send(
            new CreateVariantCommand(productA.Value!.Id, "Grande", "G", null, null, false, 50.00m, null));

        variantResult.IsSuccess.Should().BeFalse();
        variantResult.Code.Should().Be(ApiErrorCodes.ProductNotFound);
    }

    private async Task<Guid> SeedTenantAsync()
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        return tenantId;
    }
}
