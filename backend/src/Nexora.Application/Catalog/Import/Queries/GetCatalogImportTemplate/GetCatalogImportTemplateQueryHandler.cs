using Nexora.Application.Abstractions.Catalog;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Catalog.Import.Shared;
using MediatR;

namespace Nexora.Application.Catalog.Import.Queries.GetCatalogImportTemplate;

/// <summary>
/// Gera o .xlsx modelo com o cabeçalho de <see cref="CatalogImportColumns"/> e 3 linhas de exemplo
/// preenchidas (duas variações de um mesmo produto + um produto de variação única) — US-144 §10.
/// Nunca toca o banco (não é <see cref="ICommand"/>), então não passa por <c>TransactionBehavior</c>.
/// </summary>
internal sealed class GetCatalogImportTemplateQueryHandler
    : IRequestHandler<GetCatalogImportTemplateQuery, Result<CatalogImportTemplateFile>>
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly ISpreadsheetParser _parser;

    public GetCatalogImportTemplateQueryHandler(ISpreadsheetParser parser)
    {
        _parser = parser;
    }

    public Task<Result<CatalogImportTemplateFile>> Handle(GetCatalogImportTemplateQuery request, CancellationToken cancellationToken)
    {
        var headers = new[]
        {
            CatalogImportColumns.Category,
            CatalogImportColumns.Product,
            CatalogImportColumns.Description,
            CatalogImportColumns.Variant,
            CatalogImportColumns.Price,
        };

        var exampleRows = new List<IReadOnlyList<string>>
        {
            new[] { "Pizzas Salgadas", "Pizza Mussarela", "Molho, mussarela e orégano", "Broto", "35.90" },
            new[] { "Pizzas Salgadas", "Pizza Mussarela", "Molho, mussarela e orégano", "Grande", "52.90" },
            new[] { "Bebidas", "Refrigerante Lata 350ml", "Gelado", "", "6.00" },
        };

        var content = _parser.BuildTemplate(headers, exampleRows, sheetName: "Cardápio");

        var file = new CatalogImportTemplateFile(content, "modelo-importacao-cardapio.xlsx", ContentType);

        return Task.FromResult(Result<CatalogImportTemplateFile>.Success(file));
    }
}
