using Nexora.Application.Catalog.Import.Commands.ImportCatalog;
using Nexora.Application.Catalog.Import.Shared;
using Nexora.Application.Catalog.Import.Queries.ValidateCatalogImport;
using Nexora.Domain.Platform;
using Nexora.Infrastructure.Catalog;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cenários Gherkin da US-144 (Importação de cardápio por planilha) contra um PostgreSQL real
/// (Testcontainers) e o pipeline MediatR de produção — mesmo padrão de
/// <see cref="CatalogIntegrationTests"/>. Exercita <see cref="ImportCatalogCommand"/> e
/// <see cref="ValidateCatalogImportQuery"/> diretamente pelo <see cref="ISender"/>, sem host HTTP
/// (mesma decisão documentada em <c>CatalogImportController</c>: o corpo multipart já vira
/// <c>byte[]</c> antes do MediatR, então o handler não precisa de HTTP nenhum para ser testado).
/// </summary>
[Collection("Postgres")]
public sealed class CatalogImportIntegrationTests
{
    private readonly PostgresFixture _fixture;
    private static readonly ClosedXmlSpreadsheetParser Parser = new();

    public CatalogImportIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static byte[] BuildWorkbook(params IReadOnlyList<string>[] rows) =>
        Parser.BuildTemplate(CatalogImportColumns.All, rows, "Cardápio");

    private static IReadOnlyList<string> Row(string categoria, string produto, string descricao, string variacao, string preco) =>
        new[] { categoria, produto, descricao, variacao, preco };

    /// <summary>Cenário "Importação completa" (US-144 §4): categorias, produtos, variações e preços são criados, com a contagem por tipo.</summary>
    [Fact]
    public async Task Importar_Planilha_Valida_Cria_Categorias_Produtos_Variantes_E_Precos()
    {
        var tenantId = await SeedTenantAsync();
        var actorId = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(tenantId, userId: actorId);

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var file = BuildWorkbook(
            Row("Pizzas Salgadas", "Pizza Mussarela", "Molho e mussarela", "Broto", "35.90"),
            Row("Pizzas Salgadas", "Pizza Mussarela", "Molho e mussarela", "Grande", "52.90"),
            Row("Bebidas", "Refrigerante Lata", "350ml", "", "6.00"));

        var result = await sender.Send(new ImportCatalogCommand(file, "cardapio.xlsx"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Valid.Should().BeTrue();
        result.Value!.Created.Categories.Should().Be(2);
        result.Value!.Created.Products.Should().Be(2);
        result.Value!.Created.Variants.Should().Be(3);
        result.Value!.Updated.Products.Should().Be(0);

        (await db.Categories.CountAsync(c => c.TenantId == tenantId)).Should().Be(2);
        (await db.Products.CountAsync(p => p.TenantId == tenantId)).Should().Be(2);
        (await db.ProductVariants.CountAsync(v => v.TenantId == tenantId)).Should().Be(3);
        (await db.Prices.CountAsync(p => p.TenantId == tenantId)).Should().Be(3);

        // EVT-050 (US-144 §6) — um product.created por produto tocado, payload com source=IMPORT.
        var productEvents = await db.DomainEvents.Where(e => e.TenantId == tenantId && e.Type == "product.created").ToListAsync();
        productEvents.Should().HaveCount(2);
        productEvents.Should().OnlyContain(e => e.Payload.Contains("IMPORT"));
    }

    /// <summary>Cenário "Erros por linha" (US-144 §4): nenhuma linha é gravada quando há erro de validação.</summary>
    [Fact]
    public async Task Planilha_Com_Linha_Invalida_Nao_Grava_Nada()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var file = BuildWorkbook(
            Row("Pizzas Salgadas", "Pizza Mussarela", "Molho e mussarela", "Broto", "35.90"),
            Row("Bebidas", "Refrigerante Lata", "350ml", "", "não é um preço"),
            Row("Sobremesas", "Pudim", "", "", "-5.00"));

        var result = await sender.Send(new ImportCatalogCommand(file, "cardapio-com-erro.xlsx"));

        result.IsSuccess.Should().BeTrue("o handler nunca devolve Result.Failure por linha inválida — ver docstring de ImportCatalogCommandHandler");
        result.Value!.Valid.Should().BeFalse();
        result.Value!.Errors.Should().HaveCount(2);
        result.Value!.Errors.Should().Contain(e => e.Row == 3 && e.Column == "preco");
        result.Value!.Errors.Should().Contain(e => e.Row == 4 && e.Column == "preco" && e.Message.Contains("negativo"));

        (await db.Categories.CountAsync(c => c.TenantId == tenantId)).Should().Be(0);
        (await db.Products.CountAsync(p => p.TenantId == tenantId)).Should().Be(0);
        (await db.AuditLogs.CountAsync(a => a.TenantId == tenantId && a.Action == "MENU_IMPORTED")).Should().Be(0);
    }

    /// <summary>Cenário "Importação incremental" (US-144 §4): reimportar a mesma planilha atualiza, não duplica.</summary>
    [Fact]
    public async Task Reimportar_A_Mesma_Planilha_Atualiza_Em_Vez_De_Duplicar()
    {
        var tenantId = await SeedTenantAsync();
        var actorId = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(tenantId, userId: actorId);

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var firstFile = BuildWorkbook(Row("Pizzas Salgadas", "Pizza Mussarela", "Molho e mussarela", "", "35.90"));
        var firstResult = await sender.Send(new ImportCatalogCommand(firstFile, "cardapio.xlsx"));
        firstResult.Value!.Created.Products.Should().Be(1);

        // Reimportação: mesma categoria/produto, preço diferente — precisa ATUALIZAR o preço da
        // variação existente, não criar uma segunda "Pizza Mussarela".
        var secondFile = BuildWorkbook(Row("Pizzas Salgadas", "Pizza Mussarela", "Molho e mussarela", "", "39.90"));
        var secondResult = await sender.Send(new ImportCatalogCommand(secondFile, "cardapio-v2.xlsx"));

        secondResult.Value!.Valid.Should().BeTrue();
        secondResult.Value!.Created.Categories.Should().Be(0);
        secondResult.Value!.Created.Products.Should().Be(0);
        secondResult.Value!.Updated.Products.Should().Be(1);
        secondResult.Value!.Updated.Variants.Should().Be(1);

        (await db.Categories.CountAsync(c => c.TenantId == tenantId)).Should().Be(1);
        (await db.Products.CountAsync(p => p.TenantId == tenantId)).Should().Be(1);
        (await db.ProductVariants.CountAsync(v => v.TenantId == tenantId)).Should().Be(1);

        var variant = await db.ProductVariants.SingleAsync(v => v.TenantId == tenantId);
        var currentPrice = await db.Prices
            .Where(p => p.VariantId == variant.Id && p.ValidTo == null)
            .SingleAsync();
        currentPrice.Amount.Should().Be(39.90m);

        // O preço antigo foi fechado (ValidTo preenchido), não apagado — histórico preservado (US-011 §8).
        (await db.Prices.CountAsync(p => p.VariantId == variant.Id)).Should().Be(2);
    }

    /// <summary>Cenário "Registro em auditoria" (US-144 §4): audit_log recebe autor, arquivo e contagens.</summary>
    [Fact]
    public async Task Importacao_Concluida_Registra_Uma_Linha_De_Auditoria_Com_Autor_Arquivo_E_Contagens()
    {
        var tenantId = await SeedTenantAsync();
        var actorId = Guid.NewGuid();
        var tenantContext = new StaticTenantContext(tenantId, userId: actorId);

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var file = BuildWorkbook(Row("Bebidas", "Água com gás", "500ml", "", "5.00"));
        var result = await sender.Send(new ImportCatalogCommand(file, "cardapio-bebidas.xlsx"));
        result.Value!.Valid.Should().BeTrue();

        var auditLog = await db.AuditLogs.SingleAsync(a => a.TenantId == tenantId && a.Action == "MENU_IMPORTED");
        auditLog.ActorId.Should().Be(actorId);
        auditLog.Entity.Should().Be("catalog_import");
        auditLog.After.Should().Contain("cardapio-bebidas.xlsx");
        auditLog.After.Should().Contain("\"Products\":1");
    }

    /// <summary>Cenário "Pré-visualização" (US-144 §4): mostra o que seria criado/atualizado e não grava nada.</summary>
    [Fact]
    public async Task Validar_Planilha_Devolve_Preview_E_Nao_Grava_Nada()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, userId: Guid.NewGuid());

        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();

        var file = BuildWorkbook(
            Row("Pizzas Salgadas", "Pizza Mussarela", "", "Broto", "35.90"),
            Row("Pizzas Salgadas", "Pizza Mussarela", "", "Grande", "52.90"));

        var result = await sender.Send(new ValidateCatalogImportQuery(file, "cardapio.xlsx"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Valid.Should().BeTrue();
        result.Value!.Preview.ToCreate.Categories.Should().Be(1);
        result.Value!.Preview.ToCreate.Products.Should().Be(1);
        result.Value!.Preview.ToCreate.Variants.Should().Be(2);

        (await db.Categories.CountAsync(c => c.TenantId == tenantId)).Should().Be(0);
        (await db.Products.CountAsync(p => p.TenantId == tenantId)).Should().Be(0);
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
