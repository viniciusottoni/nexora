using System.Globalization;
using Nexora.Application.Abstractions.Catalog;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Import.Shared;

/// <summary>
/// Nomes de coluna do modelo de planilha (US-144 §10) — cabeçalho normalizado
/// (<c>Trim().ToLowerInvariant()</c>, ver <see cref="ISpreadsheetParser"/>). Mantido mínimo de
/// propósito (US-144 §15: "modelo de planilha complexo demais faz o cliente errar e desistir"):
/// categoria, produto, preço e descrição/variação — o suficiente para reaproveitar exatamente as
/// mesmas regras de <see cref="Product.Create"/>/<see cref="ProductVariant.Create"/>/
/// <see cref="Price.Create"/> já usadas por <c>CreateProductCommandHandler</c>/
/// <c>CreateVariantCommandHandler</c>.
/// </summary>
public static class CatalogImportColumns
{
    public const string Category = "categoria";
    public const string Product = "produto";
    public const string Description = "descricao";
    public const string Variant = "variacao";
    public const string Price = "preco";

    public static readonly IReadOnlyList<string> All = new[] { Category, Product, Description, Variant, Price };
}

/// <summary>Linha "arquivo" — usada quando o próprio arquivo não tem nenhuma linha de dados (planilha vazia).</summary>
internal static class CatalogImportGeneralError
{
    public const string Column = "arquivo";
}

/// <summary>Plano de uma variante a criar ou atualizar — resolvido a partir de UMA linha da planilha.</summary>
public sealed class CatalogImportVariantPlan
{
    /// <summary><c>null</c> => variante única implícita do produto (mesma convenção de <c>CreateProductCommandHandler</c>).</summary>
    public string? Name { get; init; }
    public required decimal Price { get; init; }
    /// <summary><c>null</c> => variante nova.</summary>
    public ProductVariant? Existing { get; init; }
}

/// <summary>Plano de um produto a criar ou atualizar — agrega uma ou mais <see cref="CatalogImportVariantPlan"/> (linhas com o mesmo produto+categoria).</summary>
public sealed class CatalogImportProductPlan
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    /// <summary><c>null</c> => produto novo.</summary>
    public Product? Existing { get; init; }
    public List<CatalogImportVariantPlan> Variants { get; } = new();
}

/// <summary>Plano de uma categoria a criar ou reaproveitar — agrega um ou mais <see cref="CatalogImportProductPlan"/>.</summary>
public sealed class CatalogImportCategoryPlan
{
    public required string Name { get; init; }
    /// <summary><c>null</c> => categoria nova.</summary>
    public Category? Existing { get; init; }
    public List<CatalogImportProductPlan> Products { get; } = new();
}

/// <summary>
/// Resultado da leitura + validação de uma planilha de importação — usado tanto por
/// <c>ValidateCatalogImportQueryHandler</c> (só leitura, monta a pré-visualização) quanto por
/// <c>ImportCatalogCommandHandler</c> (aplica as mutações quando <see cref="IsValid"/>). Nenhum dos
/// dois duplica a lógica de resolução de categoria/produto/variante — ela vive só aqui.
/// </summary>
public sealed class CatalogImportPlan
{
    public List<CatalogImportRowError> Errors { get; } = new();
    public List<CatalogImportCategoryPlan> Categories { get; } = new();

    /// <summary>
    /// Gherkin "Erros por linha" (US-144 §4): QUALQUER linha inválida invalida o arquivo inteiro —
    /// "nenhuma linha deve ser importada até a correção". Não existe importação parcial.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    public int CategoriesToCreate => Categories.Count(c => c.Existing is null);
    public int ProductsToCreate => Categories.Sum(c => c.Products.Count(p => p.Existing is null));
    public int ProductsToUpdate => Categories.Sum(c => c.Products.Count(p => p.Existing is not null));
    public int VariantsToCreate => Categories.Sum(c => c.Products.Sum(p => p.Variants.Count(v => v.Existing is null)));
    public int VariantsToUpdate => Categories.Sum(c => c.Products.Sum(p => p.Variants.Count(v => v.Existing is not null)));
}

/// <summary>
/// Constrói o <see cref="CatalogImportPlan"/> a partir da <see cref="SpreadsheetTable"/> já lida —
/// validação de campo (obrigatório/formato/negativo), deduplicação de linha dentro do mesmo
/// arquivo e resolução de categoria/produto/variante contra o que já existe no tenant (upsert por
/// nome — "código estável" desta história, ver relatório da tarefa: nenhuma coluna nova foi
/// adicionada ao schema, o nome dentro do escopo pai já é a chave natural).
/// </summary>
public static class CatalogImportPlanner
{
    /// <summary>Busca o estado atual do tenant e delega para <see cref="Build"/> — o único método com acesso a banco desta classe.</summary>
    public static async Task<CatalogImportPlan> BuildAsync(
        SpreadsheetTable table,
        Guid tenantId,
        IApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var existingCategories = await db.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var existingProducts = await db.Products
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var existingVariants = await db.ProductVariants
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.DeletedAt == null)
            .ToListAsync(cancellationToken);

        return Build(table, existingCategories, existingProducts, existingVariants);
    }

    /// <summary>
    /// Núcleo puro e síncrono do planejamento (validação de campo, deduplicação de linha e
    /// resolução de categoria/produto/variante contra o que já existe) — separado de
    /// <see cref="BuildAsync"/> de propósito, para ser testável em <c>Nexora.UnitTests</c> sem
    /// precisar de um <see cref="IApplicationDbContext"/> real (nenhum fake de dezenas de
    /// <c>DbSet&lt;T&gt;</c> só para testar validação de linha).
    /// </summary>
    public static CatalogImportPlan Build(
        SpreadsheetTable table,
        IReadOnlyList<Category> existingCategories,
        IReadOnlyList<Product> existingProducts,
        IReadOnlyList<ProductVariant> existingVariants)
    {
        var plan = new CatalogImportPlan();

        if (table.Rows.Count == 0)
        {
            plan.Errors.Add(new CatalogImportRowError(0, CatalogImportGeneralError.Column,
                "A planilha não contém nenhuma linha de dados."));
            return plan;
        }

        var categoryPlans = new Dictionary<string, CatalogImportCategoryPlan>(StringComparer.OrdinalIgnoreCase);
        var productPlans = new Dictionary<(string Category, string Product), CatalogImportProductPlan>();
        var seenRowKeys = new HashSet<(string Category, string Product, string Variant)>();

        foreach (var row in table.Rows)
        {
            var rowErrors = new List<CatalogImportRowError>();

            var categoryName = GetCell(row, CatalogImportColumns.Category);
            var productName = GetCell(row, CatalogImportColumns.Product);
            var description = GetCell(row, CatalogImportColumns.Description);
            var variantNameRaw = GetCell(row, CatalogImportColumns.Variant);
            var priceRaw = GetCell(row, CatalogImportColumns.Price);

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                rowErrors.Add(new CatalogImportRowError(row.RowNumber, CatalogImportColumns.Category, "Categoria é obrigatória."));
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                rowErrors.Add(new CatalogImportRowError(row.RowNumber, CatalogImportColumns.Product, "Produto é obrigatório."));
            }

            decimal price = 0m;
            if (string.IsNullOrWhiteSpace(priceRaw))
            {
                rowErrors.Add(new CatalogImportRowError(row.RowNumber, CatalogImportColumns.Price, "Preço é obrigatório."));
            }
            else if (!TryParsePrice(priceRaw, out price))
            {
                rowErrors.Add(new CatalogImportRowError(row.RowNumber, CatalogImportColumns.Price, "Valor inválido — use um número (ex.: 45.90)."));
            }
            else if (price < 0)
            {
                rowErrors.Add(new CatalogImportRowError(row.RowNumber, CatalogImportColumns.Price, "Preço não pode ser negativo."));
            }

            if (rowErrors.Count > 0)
            {
                plan.Errors.AddRange(rowErrors);
                continue;
            }

            var categoryNorm = categoryName!.Trim();
            var productNorm = productName!.Trim();
            var variantNorm = string.IsNullOrWhiteSpace(variantNameRaw) ? null : variantNameRaw.Trim();
            var descriptionNorm = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            var categoryKey = categoryNorm.ToLowerInvariant();
            var productKeyPart = productNorm.ToLowerInvariant();
            var variantKeyPart = (variantNorm ?? string.Empty).ToLowerInvariant();
            var rowKey = (categoryKey, productKeyPart, variantKeyPart);

            if (!seenRowKeys.Add(rowKey))
            {
                plan.Errors.Add(new CatalogImportRowError(
                    row.RowNumber,
                    CatalogImportColumns.Product,
                    "Linha duplicada — a combinação categoria/produto/variação já aparece em outra linha desta planilha."));
                continue;
            }

            if (!categoryPlans.TryGetValue(categoryNorm, out var categoryPlan))
            {
                var existingCategory = existingCategories
                    .FirstOrDefault(c => string.Equals(c.Name, categoryNorm, StringComparison.OrdinalIgnoreCase));

                categoryPlan = new CatalogImportCategoryPlan { Name = categoryNorm, Existing = existingCategory };
                categoryPlans[categoryNorm] = categoryPlan;
                plan.Categories.Add(categoryPlan);
            }

            var productKey = (categoryKey, productKeyPart);
            if (!productPlans.TryGetValue(productKey, out var productPlan))
            {
                var existingProduct = categoryPlan.Existing is not null
                    ? existingProducts.FirstOrDefault(p =>
                        p.CategoryId == categoryPlan.Existing.Id &&
                        string.Equals(p.Name, productNorm, StringComparison.OrdinalIgnoreCase))
                    : null;

                productPlan = new CatalogImportProductPlan
                {
                    Name = productNorm,
                    Description = descriptionNorm,
                    Existing = existingProduct,
                };
                productPlans[productKey] = productPlan;
                categoryPlan.Products.Add(productPlan);
            }

            var existingVariant = productPlan.Existing is not null
                ? (variantNorm is null
                    ? existingVariants.FirstOrDefault(v => v.ProductId == productPlan.Existing.Id && v.IsDefault && v.DeletedAt == null)
                    : existingVariants.FirstOrDefault(v =>
                        v.ProductId == productPlan.Existing.Id &&
                        string.Equals(v.Name, variantNorm, StringComparison.OrdinalIgnoreCase)))
                : null;

            productPlan.Variants.Add(new CatalogImportVariantPlan
            {
                Name = variantNorm,
                Price = price,
                Existing = existingVariant,
            });
        }

        return plan;
    }

    private static string? GetCell(SpreadsheetRow row, string column) =>
        row.Cells.TryGetValue(column, out var value) ? value : null;

    /// <summary>
    /// Aceita o formato invariante estrito (<c>45.90</c>) e o pt-BR simples (<c>45,90</c>) — nessa
    /// ordem, SEM <see cref="NumberStyles.AllowThousands"/>: com ele, <c>decimal.TryParse("45,90",
    /// NumberStyles.Number, CultureInfo.InvariantCulture, ...)</c> "funciona", mas lê a vírgula como
    /// separador de milhar e devolve <c>4590</c> em vez de <c>45.90</c> — um erro silencioso de 100x
    /// no preço, exatamente o que ADR-017 proíbe ("nunca coagir silenciosamente"). Por isso o
    /// fallback pt-BR só aceita o padrão simples <c>\d+,\d{1,2}</c> (sem separador de milhar) — a
    /// planilha com milhar formatado (<c>1.234,56</c>) é ambígua demais para arriscar; o modelo já
    /// orienta o formato com ponto.
    /// </summary>
    public static bool TryParsePrice(string raw, out decimal price)
    {
        var cleaned = raw.Trim();
        if (cleaned.StartsWith("R$", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[2..].Trim();
        }

        const NumberStyles strictStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign
            | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

        if (decimal.TryParse(cleaned, strictStyle, CultureInfo.InvariantCulture, out price))
        {
            return true;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^\d+,\d{1,2}$"))
        {
            return decimal.TryParse(cleaned.Replace(',', '.'), strictStyle, CultureInfo.InvariantCulture, out price);
        }

        price = 0m;
        return false;
    }
}
