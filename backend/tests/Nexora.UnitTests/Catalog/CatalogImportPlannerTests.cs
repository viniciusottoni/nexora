using Nexora.Application.Abstractions.Catalog;
using Nexora.Application.Catalog.Import.Shared;
using Nexora.Domain.Catalog;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Catalog;

/// <summary>
/// US-144 (Importação de cardápio por planilha) §12 ("Unitário: validação de cada tipo de erro
/// possível") — cobre <see cref="CatalogImportPlanner.Build"/> isolado de banco/HTTP (o núcleo puro
/// e síncrono, ver docstring do método) e <see cref="CatalogImportPlanner.TryParsePrice"/>.
/// </summary>
public sealed class CatalogImportPlannerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static SpreadsheetTable TableOf(params IReadOnlyDictionary<string, string?>[] rows)
    {
        var spreadsheetRows = rows
            .Select((cells, index) => new SpreadsheetRow(index + 2, cells))
            .ToList();

        return new SpreadsheetTable(CatalogImportColumns.All, spreadsheetRows);
    }

    private static Dictionary<string, string?> Row(
        string? categoria = "Pizzas", string? produto = "Mussarela", string? descricao = null,
        string? variacao = null, string? preco = "45.90") =>
        new()
        {
            [CatalogImportColumns.Category] = categoria,
            [CatalogImportColumns.Product] = produto,
            [CatalogImportColumns.Description] = descricao,
            [CatalogImportColumns.Variant] = variacao,
            [CatalogImportColumns.Price] = preco,
        };

    private static readonly IReadOnlyList<Category> NoCategories = Array.Empty<Category>();
    private static readonly IReadOnlyList<Product> NoProducts = Array.Empty<Product>();
    private static readonly IReadOnlyList<ProductVariant> NoVariants = Array.Empty<ProductVariant>();

    [Fact]
    public void Planilha_Sem_Nenhuma_Linha_De_Dados_E_Invalida()
    {
        var table = new SpreadsheetTable(CatalogImportColumns.All, Array.Empty<SpreadsheetRow>());

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.IsValid.Should().BeFalse();
        plan.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Linha_Sem_Categoria_Gera_Erro_Na_Coluna_Categoria()
    {
        var table = TableOf(Row(categoria: null));

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.IsValid.Should().BeFalse();
        plan.Errors.Should().ContainSingle(e => e.Column == CatalogImportColumns.Category && e.Row == 2);
    }

    [Fact]
    public void Linha_Sem_Produto_Gera_Erro_Na_Coluna_Produto()
    {
        var table = TableOf(Row(produto: "   "));

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.IsValid.Should().BeFalse();
        plan.Errors.Should().ContainSingle(e => e.Column == CatalogImportColumns.Product);
    }

    [Fact]
    public void Linha_Sem_Preco_Gera_Erro_De_Campo_Obrigatorio()
    {
        var table = TableOf(Row(preco: null));

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.Errors.Should().ContainSingle(e => e.Column == CatalogImportColumns.Price && e.Message.Contains("obrigat"));
    }

    [Fact]
    public void Preco_Nao_Numerico_Gera_Erro_De_Formato()
    {
        var table = TableOf(Row(preco: "quarenta e cinco"));

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.IsValid.Should().BeFalse();
        plan.Errors.Should().ContainSingle(e => e.Column == CatalogImportColumns.Price && e.Message.Contains("inválido"));
    }

    [Fact]
    public void Preco_Negativo_Gera_Erro_Dedicado_Mesmo_Sendo_Numero_Valido()
    {
        var table = TableOf(Row(preco: "-10.00"));

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.IsValid.Should().BeFalse();
        plan.Errors.Should().ContainSingle(e => e.Column == CatalogImportColumns.Price && e.Message.Contains("negativo"));
    }

    [Fact]
    public void Linha_Duplicada_Na_Mesma_Planilha_E_Rejeitada()
    {
        var table = TableOf(
            Row(categoria: "Pizzas", produto: "Mussarela", variacao: "Grande", preco: "45.90"),
            Row(categoria: "Pizzas", produto: "Mussarela", variacao: "Grande", preco: "50.00"));

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.IsValid.Should().BeFalse();
        plan.Errors.Should().ContainSingle(e => e.Row == 3 && e.Message.Contains("duplicada"));
    }

    [Fact]
    public void Mesmo_Produto_Categoria_Com_Variacoes_Diferentes_Nao_E_Duplicata()
    {
        var table = TableOf(
            Row(categoria: "Pizzas", produto: "Mussarela", variacao: "Broto", preco: "35.90"),
            Row(categoria: "Pizzas", produto: "Mussarela", variacao: "Grande", preco: "52.90"));

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.IsValid.Should().BeTrue();
        plan.Categories.Should().ContainSingle();
        plan.Categories[0].Products.Should().ContainSingle();
        plan.Categories[0].Products[0].Variants.Should().HaveCount(2);
    }

    [Fact]
    public void Planilha_Toda_Nova_Marca_Categoria_Produto_E_Variante_Como_Novos()
    {
        var table = TableOf(Row(categoria: "Bebidas", produto: "Refrigerante", variacao: null, preco: "6.00"));

        var plan = CatalogImportPlanner.Build(table, NoCategories, NoProducts, NoVariants);

        plan.IsValid.Should().BeTrue();
        plan.CategoriesToCreate.Should().Be(1);
        plan.ProductsToCreate.Should().Be(1);
        plan.VariantsToCreate.Should().Be(1);
        plan.ProductsToUpdate.Should().Be(0);
        plan.VariantsToUpdate.Should().Be(0);
    }

    [Fact]
    public void Categoria_E_Produto_Ja_Existentes_Sao_Reaproveitados_Como_Atualizacao()
    {
        var existingCategory = Category.Create(TenantId, "Pizzas");
        var existingProduct = Product.Create(TenantId, existingCategory.Id, "Mussarela");
        var existingVariant = ProductVariant.Create(TenantId, existingProduct.Id, "Mussarela", isDefault: true);

        var table = TableOf(Row(categoria: "pizzas", produto: "MUSSARELA", preco: "48.00"));

        var plan = CatalogImportPlanner.Build(
            table,
            new[] { existingCategory },
            new[] { existingProduct },
            new[] { existingVariant });

        plan.IsValid.Should().BeTrue();
        plan.CategoriesToCreate.Should().Be(0, "categoria já existe (comparação por nome, sem diferenciar maiúsculas/minúsculas)");
        plan.ProductsToCreate.Should().Be(0);
        plan.ProductsToUpdate.Should().Be(1);
        plan.VariantsToCreate.Should().Be(0);
        plan.VariantsToUpdate.Should().Be(1);

        var resolvedVariant = plan.Categories.Single().Products.Single().Variants.Single();
        resolvedVariant.Existing.Should().BeSameAs(existingVariant);
    }

    [Fact]
    public void Categoria_Existente_Com_Produto_Novo_Cria_Só_O_Produto()
    {
        var existingCategory = Category.Create(TenantId, "Pizzas");

        var table = TableOf(Row(categoria: "Pizzas", produto: "Calabresa", preco: "48.00"));

        var plan = CatalogImportPlanner.Build(table, new[] { existingCategory }, NoProducts, NoVariants);

        plan.IsValid.Should().BeTrue();
        plan.CategoriesToCreate.Should().Be(0);
        plan.ProductsToCreate.Should().Be(1);
    }

    [Theory]
    [InlineData("45.90", "45.90")]
    [InlineData("45,90", "45.90")]
    [InlineData("R$ 45.90", "45.90")]
    [InlineData("6", "6")]
    public void TryParsePrice_Aceita_Formatos_Suportados(string raw, string expected)
    {
        var success = CatalogImportPlanner.TryParsePrice(raw, out var price);

        success.Should().BeTrue();
        price.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TryParsePrice_Nunca_Interpreta_Virgula_Como_Separador_De_Milhar()
    {
        // Regressão: NumberStyles.Number aceitaria "45,90" como separador de milhar e devolveria
        // 4590 em vez de 45.90 — um erro silencioso de 100x, proibido pelo ADR-017.
        CatalogImportPlanner.TryParsePrice("45,90", out var price).Should().BeTrue();
        price.Should().Be(45.90m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.234,56")]
    public void TryParsePrice_Rejeita_Formatos_Ambiguos_Ou_Invalidos(string raw)
    {
        CatalogImportPlanner.TryParsePrice(raw, out _).Should().BeFalse();
    }
}
