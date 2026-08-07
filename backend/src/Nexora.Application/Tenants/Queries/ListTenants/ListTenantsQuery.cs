using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;

namespace Nexora.Application.Tenants.Queries.ListTenants;

/// <summary>
/// US-151 "Diretório de estabelecimentos com busca e filtros" — porta de
/// <c>GET /v1/platform/tenants</c>. Todos os filtros são opcionais e combináveis (mesmo espírito de
/// <c>GetAuditLogQuery</c>); os repetíveis (<paramref name="Statuses"/>/<paramref name="Plans"/>/
/// <paramref name="Templates"/>/<paramref name="HealthStatuses"/>) chegam já como lista, vazia
/// quando o parâmetro não foi informado (nunca nula) — mais simples de compor no handler e de
/// ecoar em <c>appliedFilters</c>.
/// </summary>
/// <param name="SearchTerm">Busca <c>ILIKE</c> contra nome/slug/domínio/documento/e-mail do dono.</param>
/// <param name="Statuses">Filtro por <see cref="TenantStatus"/> (repetível, OR entre os valores).</param>
/// <param name="Plans">Filtro por plano — string livre, sem catálogo fixo ainda (US-154).</param>
/// <param name="Templates">Filtro por <see cref="Tenant.TemplateCode"/> (código do modelo de negócio).</param>
/// <param name="HealthStatuses">Filtro por <see cref="TenantHealthStatus"/> agregado do tenant.</param>
/// <param name="CreatedFrom">Início (inclusive) do intervalo sobre <see cref="Tenant.CreatedAt"/>.</param>
/// <param name="CreatedTo">Fim (inclusive) do intervalo sobre <see cref="Tenant.CreatedAt"/>.</param>
/// <param name="Sort">Critério de ordenação — default <see cref="TenantDirectorySort.Attention"/>.</param>
/// <param name="Limit">Tamanho da página (máximo 100).</param>
/// <param name="Cursor">Cursor opaco da página anterior — nulo/vazio pede a primeira página.</param>
public sealed record ListTenantsQuery(
    string? SearchTerm,
    IReadOnlyList<TenantStatus> Statuses,
    IReadOnlyList<string> Plans,
    IReadOnlyList<string> Templates,
    IReadOnlyList<TenantHealthStatus> HealthStatuses,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    TenantDirectorySort Sort,
    int Limit,
    string? Cursor) : IQuery<TenantDirectoryListResponse>;
