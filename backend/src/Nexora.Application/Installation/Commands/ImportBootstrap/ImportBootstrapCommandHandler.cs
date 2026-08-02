using System.Text.Json;
using System.Text.Json.Nodes;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installation.Abstractions;
using Nexora.Contracts.Installation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Installation.Commands.ImportBootstrap;

/// <summary>
/// Handler único e atômico (uma transação — ADR-006) que grava identidade/configuração do
/// tenant e conclui o pareamento da instalação local. Catálogo e autorização são delegados a
/// <see cref="IBootstrapCatalogImporter"/>/<see cref="IBootstrapAuthorizationImporter"/> — ver
/// nota de escopo nessas interfaces.
/// </summary>
internal sealed class ImportBootstrapCommandHandler
    : IRequestHandler<ImportBootstrapCommand, Result<ImportBootstrapResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IBootstrapCatalogImporter _catalogImporter;
    private readonly IBootstrapAuthorizationImporter _authorizationImporter;
    private readonly ILogger<ImportBootstrapCommandHandler> _logger;

    public ImportBootstrapCommandHandler(
        IApplicationDbContext db,
        IBootstrapCatalogImporter catalogImporter,
        IBootstrapAuthorizationImporter authorizationImporter,
        ILogger<ImportBootstrapCommandHandler> logger)
    {
        _db = db;
        _catalogImporter = catalogImporter;
        _authorizationImporter = authorizationImporter;
        _logger = logger;
    }

    public async Task<Result<ImportBootstrapResponse>> Handle(
        ImportBootstrapCommand request,
        CancellationToken cancellationToken)
    {
        var first = request.ConfigPages[0];
        var pagesDivergem = request.ConfigPages.Any(page =>
            page.ConfigVersion != first.ConfigVersion || page.CatalogVersion != first.CatalogVersion);

        if (pagesDivergem)
        {
            return Result<ImportBootstrapResponse>.Failure(
                "Carga inicial contém versões de configuração divergentes.",
                ApiErrorCodes.InstallationBootstrapVersionMismatch);
        }

        var tenantId = request.Tenant.Id;
        var storeId = request.Store.Id;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            tenant = Tenant.Create(tenantId, request.Tenant.Slug, request.Tenant.Name, request.Store.Timezone);
            _db.Tenants.Add(tenant);
        }
        else
        {
            tenant.ApplyBootstrapIdentity(request.Tenant.Name, request.Tenant.Slug, request.Store.Timezone);
        }

        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId, cancellationToken);
        if (store is null)
        {
            store = Store.Create(storeId, tenantId, request.Store.Name, isDefault: true, timezone: request.Store.Timezone);
            _db.Stores.Add(store);
        }
        else
        {
            store.ApplyBootstrapIdentity(request.Store.Name, request.Store.Timezone);
        }

        var config = await _db.TenantConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        if (config is null)
        {
            config = TenantConfig.Create(tenantId);
            _db.TenantConfigs.Add(config);
        }

        config.ApplyBootstrap(
            first.ConfigVersion,
            first.CatalogVersion,
            first.Branding.GetRawText(),
            first.Operation.GetRawText(),
            first.Thresholds.GetRawText(),
            first.Modules.GetRawText(),
            first.Fiscal.GetRawText(),
            first.Printers.GetRawText(),
            first.Payments.GetRawText(),
            first.Maintenance.GetRawText());

        var installationId = request.Installation.Id;
        var installation = await _db.EdgeInstallations.FirstOrDefaultAsync(e => e.Id == installationId, cancellationToken);
        if (installation is null)
        {
            installation = EdgeInstallation.CreateInstalled(
                installationId, tenantId, storeId,
                request.Installation.Label ?? "Edge local",
                request.Installation.PublicKey,
                request.Installation.Version);
            _db.EdgeInstallations.Add(installation);
        }
        else
        {
            installation.CompleteRegistration(request.Installation.PublicKey, request.Installation.Version, request.Installation.Label);
        }

        var mergedCatalog = MergeJsonSections(request.ConfigPages.Select(p => p.Catalog));
        if (mergedCatalog is not null)
        {
            await _catalogImporter.ImportAsync(tenantId, storeId, mergedCatalog, cancellationToken);
        }

        var mergedAuthorization = MergeJsonSections(request.ConfigPages.Select(p => p.Authorization));
        if (mergedAuthorization is not null)
        {
            await _authorizationImporter.ImportAsync(tenantId, storeId, mergedAuthorization, cancellationToken);
        }

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.bootstrap_imported",
            aggregateType: nameof(EdgeInstallation),
            aggregateId: installationId,
            payload: $$"""{"configVersion":{{first.ConfigVersion}},"catalogVersion":{{first.CatalogVersion}}}""",
            origin: "EDGE",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: storeId,
            installationId: installationId));

        // SaveChangesAsync é feito pelo TransactionBehavior (commands).

        _logger.LogInformation(
            "Bootstrap importado. TenantId={TenantId}, StoreId={StoreId}, InstallationId={InstallationId}, ConfigVersion={ConfigVersion}",
            tenantId, storeId, installationId, first.ConfigVersion);

        return Result<ImportBootstrapResponse>.Success(new ImportBootstrapResponse(
            tenantId, storeId, installationId, first.ConfigVersion, first.CatalogVersion));
    }

    /// <summary>
    /// Concatena, por chave, os arrays de nível superior de cada página (mesmo papel do
    /// <c>mergePages</c> genérico do TS) — sem interpretar o conteúdo dos itens, já que o
    /// esquema de catálogo/autorização pertence a outros módulos.
    /// </summary>
    private static string? MergeJsonSections(IEnumerable<JsonElement?> pages)
    {
        var present = pages.Where(p => p is not null).Select(p => p!.Value).ToList();
        if (present.Count == 0)
        {
            return null;
        }

        var merged = new JsonObject();
        foreach (var page in present)
        {
            foreach (var property in page.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                if (merged[property.Name] is not JsonArray array)
                {
                    array = new JsonArray();
                    merged[property.Name] = array;
                }

                foreach (var item in property.Value.EnumerateArray())
                {
                    array.Add(JsonNode.Parse(item.GetRawText()));
                }
            }
        }

        return merged.ToJsonString();
    }
}
