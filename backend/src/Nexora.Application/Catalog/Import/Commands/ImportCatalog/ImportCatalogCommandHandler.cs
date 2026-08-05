using System.Text.Json;
using Nexora.Application.Abstractions.Catalog;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Import.Shared;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Import.Commands.ImportCatalog;

/// <summary>
/// Aplica a importação de cardápio (US-144). Reparseia e revalida a planilha do zero — nunca
/// confia no resultado de uma chamada anterior a <c>POST /v1/catalog/import/validate</c> (nenhum
/// estado é carregado entre requisições, por design: um segundo parse+validate completo é mais
/// simples e correto do que um cache de curta duração).
/// </summary>
/// <remarks>
/// <b>Por que este handler nunca devolve <see cref="Result{T}.Failure"/> para linha inválida:</b> o
/// <c>TransactionBehavior</c> decide commit/rollback olhando só para <c>Result.IsFailure</c> — mas
/// aqui o "falhou" (linha inválida) e o "não fiz nada" já são garantidos pela ausência de qualquer
/// <c>_db.Xxx.Add</c> no ramo <c>!plan.IsValid</c> abaixo (nada foi adicionado ao change tracker,
/// então o <c>SaveChangesAsync</c> do behavior não tem o que persistir — commit de uma transação
/// vazia é inofensivo). Como <see cref="ICommand{TResponse}"/> exige <c>Result&lt;T&gt;</c> e T
/// (<see cref="CatalogImportCommitResponse"/>) já modela os dois desfechos via
/// <see cref="CatalogImportCommitResponse.Valid"/>, devolver sempre <c>Result.Success</c> evita
/// forçar a lista de erros por linha (linha+coluna+mensagem) dentro do canal genérico
/// <c>IReadOnlyDictionary&lt;string,string[]&gt;</c> de <c>Result.Errors</c> — decisão deliberada,
/// documentada also na docstring do <c>ImportCatalogCommand</c>. <c>CatalogImportController</c>
/// inspeciona <c>Valid</c> para decidir 201 vs 422, sem passar pelo <c>ResultExtensions</c>
/// genérico para esse caso específico.
/// </remarks>
internal sealed class ImportCatalogCommandHandler : IRequestHandler<ImportCatalogCommand, Result<CatalogImportCommitResponse>>
{
    private static readonly CatalogImportCounts EmptyCounts = new(0, 0, 0);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ISpreadsheetParser _parser;

    public ImportCatalogCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, ISpreadsheetParser parser)
    {
        _db = db;
        _tenantContext = tenantContext;
        _parser = parser;
    }

    public async Task<Result<CatalogImportCommitResponse>> Handle(ImportCatalogCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<CatalogImportCommitResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        SpreadsheetTable table;
        try
        {
            using var stream = new MemoryStream(request.FileContent);
            table = _parser.Parse(stream);
        }
        catch (InvalidOperationException ex)
        {
            return Result<CatalogImportCommitResponse>.Failure(
                $"Não foi possível ler o arquivo enviado. Envie uma planilha .xlsx no modelo disponibilizado. ({ex.Message})",
                ApiErrorCodes.CatalogImportInvalidFile);
        }

        var plan = await CatalogImportPlanner.BuildAsync(table, tenantId, _db, cancellationToken);

        if (!plan.IsValid)
        {
            // Gherkin "Erros por linha" (US-144 §4): "nenhuma linha deve ser importada até a
            // correção" — nenhum _db.Xxx.Add foi chamado neste ramo, então não há nada para o
            // SaveChangesAsync do TransactionBehavior persistir.
            return Result<CatalogImportCommitResponse>.Success(new CatalogImportCommitResponse(
                Valid: false,
                Errors: plan.Errors,
                Created: EmptyCounts,
                Updated: EmptyCounts,
                Skipped: 0));
        }

        var categoriesCreated = 0;
        var productsCreated = 0;
        var productsUpdated = 0;
        var variantsCreated = 0;
        var variantsUpdated = 0;

        // EVT-050 (US-144 §6): um product.created/product.updated por PRODUTO tocado (não por
        // linha/variante) — payload leva source=IMPORT para diferenciar de uma edição manual.
        var touchedProducts = new List<(Guid ProductId, bool IsNew)>();

        foreach (var categoryPlan in plan.Categories)
        {
            var category = categoryPlan.Existing;
            if (category is null)
            {
                category = Category.Create(tenantId, categoryPlan.Name);
                _db.Categories.Add(category);
                categoriesCreated++;
            }

            foreach (var productPlan in categoryPlan.Products)
            {
                var product = productPlan.Existing;
                if (product is null)
                {
                    product = Product.Create(tenantId, category.Id, productPlan.Name, description: productPlan.Description);
                    _db.Products.Add(product);
                    productsCreated++;
                    touchedProducts.Add((product.Id, IsNew: true));
                }
                else
                {
                    if (!string.Equals(product.Description, productPlan.Description, StringComparison.Ordinal))
                    {
                        product.UpdateDetails(
                            product.Name,
                            product.CategoryId,
                            product.StationId,
                            productPlan.Description,
                            product.IngredientsText,
                            product.Allergens,
                            product.AllowsFractions,
                            product.MaxFractions,
                            product.SortOrder);
                    }

                    // Conta como "atualizado" mesmo quando nenhum campo mudou de valor — a linha
                    // da planilha corresponde a um produto já existente, e é isso que a
                    // pré-visualização (toUpdate) já prometeu ao usuário antes da confirmação.
                    productsUpdated++;
                    touchedProducts.Add((product.Id, IsNew: false));
                }

                foreach (var variantPlan in productPlan.Variants)
                {
                    var variant = variantPlan.Existing;
                    if (variant is null)
                    {
                        variant = ProductVariant.Create(
                            tenantId,
                            product.Id,
                            variantPlan.Name ?? product.Name,
                            isDefault: variantPlan.Name is null);
                        _db.ProductVariants.Add(variant);

                        var newPrice = Price.Create(tenantId, variant.Id, Channel.DineIn, variantPlan.Price, actorId, now);
                        _db.Prices.Add(newPrice);
                        variantsCreated++;
                    }
                    else
                    {
                        // Mesma lógica de no-op de SetVariantPriceCommandHandler (US-011): só fecha
                        // a linha vigente e cria uma nova quando o valor de fato muda.
                        var currentPrice = await _db.Prices
                            .AsNoTracking()
                            .Where(p => p.VariantId == variant.Id && p.Channel == Channel.DineIn && p.ValidTo == null)
                            .OrderByDescending(p => p.ValidFrom)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (currentPrice is null || currentPrice.Amount != variantPlan.Price)
                        {
                            if (currentPrice is not null)
                            {
                                await _db.Prices
                                    .Where(p => p.Id == currentPrice.Id && p.ValidTo == null)
                                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ValidTo, now), cancellationToken);
                            }

                            var newPrice = Price.Create(tenantId, variant.Id, Channel.DineIn, variantPlan.Price, actorId, now);
                            _db.Prices.Add(newPrice);
                        }

                        variantsUpdated++;
                    }
                }
            }
        }

        foreach (var (productId, isNew) in touchedProducts)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: isNew ? "product.created" : "product.updated",
                aggregateType: "product",
                aggregateId: productId,
                payload: JsonSerializer.Serialize(new { productId, source = "IMPORT" }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        var created = new CatalogImportCounts(categoriesCreated, productsCreated, variantsCreated);
        var updated = new CatalogImportCounts(0, productsUpdated, variantsUpdated);

        // Registro único de auditoria (US-144 §3.1/§8, action=MENU_IMPORTED) — autor, arquivo e
        // contagens, mesmo padrão de AuditLog.Create já usado por RecordSupportAccessCommandHandler.
        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "MENU_IMPORTED",
            entity: "catalog_import",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            after: JsonSerializer.Serialize(new
            {
                fileName = request.FileName,
                created,
                updated,
            })));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<CatalogImportCommitResponse>.Success(new CatalogImportCommitResponse(
            Valid: true,
            Errors: Array.Empty<CatalogImportRowError>(),
            Created: created,
            Updated: updated,
            Skipped: 0));
    }
}
