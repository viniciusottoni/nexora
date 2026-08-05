using Nexora.Application.Abstractions.Catalog;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Import.Shared;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;

namespace Nexora.Application.Catalog.Import.Queries.ValidateCatalogImport;

/// <summary>
/// Lê e valida a planilha SEM gravar nada (US-144 §7) — reusa exatamente a mesma
/// <see cref="CatalogImportPlanner"/> que <c>ImportCatalogCommandHandler</c> usa para o commit, de
/// modo que "o que a pré-visualização mostra" e "o que a importação de fato faz" nunca podem
/// divergir por bug de lógica duplicada.
/// </summary>
internal sealed class ValidateCatalogImportQueryHandler
    : IRequestHandler<ValidateCatalogImportQuery, Result<CatalogImportValidateResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ISpreadsheetParser _parser;

    public ValidateCatalogImportQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, ISpreadsheetParser parser)
    {
        _db = db;
        _tenantContext = tenantContext;
        _parser = parser;
    }

    public async Task<Result<CatalogImportValidateResponse>> Handle(ValidateCatalogImportQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<CatalogImportValidateResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        SpreadsheetTable table;
        try
        {
            using var stream = new MemoryStream(request.FileContent);
            table = _parser.Parse(stream);
        }
        catch (InvalidOperationException ex)
        {
            return Result<CatalogImportValidateResponse>.Failure(
                $"Não foi possível ler o arquivo enviado. Envie uma planilha .xlsx no modelo disponibilizado. ({ex.Message})",
                ApiErrorCodes.CatalogImportInvalidFile);
        }

        var plan = await CatalogImportPlanner.BuildAsync(table, _tenantContext.TenantId.Value, _db, cancellationToken);

        var preview = new CatalogImportPreview(
            ToCreate: new CatalogImportCounts(plan.CategoriesToCreate, plan.ProductsToCreate, plan.VariantsToCreate),
            ToUpdate: new CatalogImportCounts(Categories: 0, plan.ProductsToUpdate, plan.VariantsToUpdate));

        return Result<CatalogImportValidateResponse>.Success(new CatalogImportValidateResponse(plan.IsValid, plan.Errors, preview));
    }
}
