using System.Text.Json;

namespace Nexora.Contracts.Installation;

/// <summary>
/// Corpo do POST /v1/installation/bootstrap/import (edge) — carga de primeira subida do
/// servidor local (ADR-019). Diferente do script original (<c>import-bootstrap.ts</c>, que lia
/// arquivos do disco montado pelo provisionamento), aqui o processo de instalação/compose
/// entrega o payload já parseado no corpo da requisição: mantém o app sem I/O de arquivo
/// arbitrário e torna o comando testável como qualquer outro (decisão de design — ver relatório
/// da portabilidade).
/// </summary>
/// <summary>
/// <see cref="ConfigPages"/> é uma lista porque o cliente de instalação pode ter percorrido
/// mais de uma página de GET /v1/sync/pull (catálogo grande) antes de chamar este endpoint —
/// mesma razão de existir do <c>mergePages</c>/array de <c>events</c> no script original.
/// </summary>
public sealed record ImportBootstrapRequest(
    BootstrapTenantIdentity Tenant,
    BootstrapStoreIdentity Store,
    BootstrapInstallationIdentity Installation,
    IReadOnlyList<BootstrapConfigSection> ConfigPages);

public sealed record BootstrapTenantIdentity(Guid Id, string Name, string Slug);

public sealed record BootstrapStoreIdentity(Guid Id, string Name, string Timezone);

/// <summary>Identidade local do container edge — nunca vem da nuvem, é gerada/lida no próprio host.</summary>
public sealed record BootstrapInstallationIdentity(Guid Id, string PublicKey, string Version, string? Label);

/// <summary>
/// Seções de configuração livres (JSONB no banco — ver TODOs de value object tipado em
/// <c>Nexora.Domain.Platform.TenantConfig</c>) mais os blocos de catálogo/autorização,
/// que este módulo não interpreta (ver <c>IBootstrapCatalogImporter</c>/<c>IBootstrapAuthorizationImporter</c>).
/// </summary>
public sealed record BootstrapConfigSection(
    int ConfigVersion,
    int CatalogVersion,
    JsonElement Branding,
    JsonElement Operation,
    JsonElement Thresholds,
    JsonElement Modules,
    JsonElement Fiscal,
    JsonElement Printers,
    JsonElement Payments,
    JsonElement Maintenance,
    JsonElement? Catalog,
    JsonElement? Authorization);

public sealed record ImportBootstrapResponse(
    Guid TenantId,
    Guid StoreId,
    Guid InstallationId,
    int ConfigVersion,
    int CatalogVersion);
