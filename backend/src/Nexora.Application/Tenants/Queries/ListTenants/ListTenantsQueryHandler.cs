using System.Globalization;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.ListTenants;

/// <summary>
/// US-151 "Diretório de estabelecimentos com busca e filtros". <c>tenant</c> é a única tabela sem
/// RLS (raiz global) — busca/filtro/ordenação/cursor rodam inteiramente nela. <c>storesCount</c>/
/// <c>installationsCount</c>/<c>health</c> exigem <c>app.tenant_id</c> fixado (RLS de
/// <c>store</c>/<c>edge_installation</c>), então são agregados só para os tenants da PÁGINA atual —
/// nunca para a base inteira (mesmo padrão de <c>GetPlatformSummaryQueryHandler</c>, mas restrito ao
/// <c>Limit</c> em vez de todos os tenants).
/// </summary>
internal sealed class ListTenantsQueryHandler : IRequestHandler<ListTenantsQuery, Result<TenantDirectoryListResponse>>
{
    /// <summary>
    /// <c>health</c> não é coluna de <c>tenant</c> — só existe depois de agregar
    /// <c>edge_installation</c> por tenant. Quando o filtro é usado, a única forma de manter a
    /// promessa "nunca agregar a base inteira" é filtrar em lotes keyset (avança pelo mesmo cursor
    /// da ordenação escolhida) até formar uma página cheia ou esgotar candidatos — este teto limita
    /// o pior caso (nenhum tenant do lote bate o filtro) a <see cref="MaxHealthFilterBatches"/> ×
    /// <see cref="HealthFilterBatchSize"/> tenants agregados por requisição. US-151 §15 já assume
    /// que busca/filtro sem índice adequado "degrada com o crescimento; medir antes de escolher" —
    /// mesma ressalva vale aqui: acima de poucas centenas de tenants, este caminho pede um índice
    /// dedicado (ou a view de diretório cogitada em §8), não coberto por esta US.
    /// </summary>
    private const int MaxHealthFilterBatches = 20;
    private const int HealthFilterBatchSize = 50;

    private readonly IApplicationDbContext _db;

    public ListTenantsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantDirectoryListResponse>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedSearch = string.IsNullOrWhiteSpace(request.SearchTerm) ? null : request.SearchTerm.Trim();
        var cursorValue = TenantDirectoryCursor.Decode(request.Cursor, request.Sort);

        List<TenantDirectoryRow> rows;
        bool hasMore;

        if (request.HealthStatuses.Count == 0)
        {
            var query = ApplyOrder(ApplyCursor(BuildBaseQuery(request, normalizedSearch), request.Sort, cursorValue), request.Sort);
            var candidates = await query.AsNoTracking().Take(request.Limit + 1).ToListAsync(cancellationToken);

            hasMore = candidates.Count > request.Limit;
            var page = hasMore ? candidates.Take(request.Limit).ToList() : candidates;
            rows = await BuildRowsAsync(page, now, cancellationToken);
        }
        else
        {
            (rows, hasMore) = await FetchWithHealthFilterAsync(request, normalizedSearch, cursorValue, now, cancellationToken);
        }

        var data = rows.Select(r => new TenantDirectoryEntryResponse(
                r.Tenant.Id,
                r.Tenant.Name,
                r.Tenant.Slug,
                r.Tenant.Status.ToWireLabel(),
                r.Tenant.Plan,
                r.Tenant.OwnerEmail,
                r.StoresCount,
                r.InstallationsCount,
                r.Health.ToWireLabel(),
                r.Tenant.CreatedAt,
                r.Tenant.UpdatedAt))
            .ToList();

        var nextCursor = hasMore && rows.Count > 0
            ? TenantDirectoryCursor.Encode(request.Sort, BuildPrimary(request.Sort, rows[^1].Tenant), rows[^1].Tenant.Id)
            : null;

        var appliedFilters = new TenantDirectoryAppliedFiltersResponse(
            normalizedSearch,
            request.Statuses.Select(s => s.ToWireLabel()).ToList(),
            request.Plans.ToList(),
            request.Templates.ToList(),
            request.HealthStatuses.Select(h => h.ToWireLabel()).ToList(),
            request.CreatedFrom,
            request.CreatedTo,
            request.Sort.ToWireLabel(),
            request.Limit);

        return Result<TenantDirectoryListResponse>.Success(new TenantDirectoryListResponse(data, nextCursor, appliedFilters));
    }

    private IQueryable<Tenant> BuildBaseQuery(ListTenantsQuery request, string? normalizedSearch)
    {
        IQueryable<Tenant> query = normalizedSearch is null
            ? _db.Tenants
            : _db.TenantsMatchingSearchTerm(normalizedSearch);

        query = query.Where(t => t.DeletedAt == null);

        if (request.Statuses.Count > 0)
        {
            query = query.Where(t => request.Statuses.Contains(t.Status));
        }

        if (request.Plans.Count > 0)
        {
            query = query.Where(t => request.Plans.Contains(t.Plan));
        }

        if (request.Templates.Count > 0)
        {
            query = query.Where(t => t.TemplateCode != null && request.Templates.Contains(t.TemplateCode));
        }

        if (request.CreatedFrom is { } from)
        {
            query = query.Where(t => t.CreatedAt >= from);
        }

        if (request.CreatedTo is { } to)
        {
            query = query.Where(t => t.CreatedAt <= to);
        }

        return query;
    }

    private static IQueryable<Tenant> ApplyOrder(IQueryable<Tenant> query, TenantDirectorySort sort) => sort switch
    {
        TenantDirectorySort.Name => query.OrderBy(t => t.Name).ThenBy(t => t.Id),
        TenantDirectorySort.CreatedAt => query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id),
        TenantDirectorySort.UpdatedAt => query.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id),
        // "Criticidade" (US-151 §7/§15, ranking estendido pela US-153) — mesma semântica de
        // GetPlatformSummaryQueryHandler: SUSPENDED é o único status que exige ação do
        // administrador. Ranking traduzido para SQL via CASE (expressão condicional do C#, não
        // avaliado em memória) — SUSPENDED(0) < PROVISIONED(1) < INSTALLING(2) < ACTIVE(3) <
        // CANCELLED(4), desempate por UpdatedAt desc, depois Id desc (estabilidade). Espelha
        // TenantAttentionRanking.RankOf (não pode ser chamado aqui, ver docstring da classe).
        TenantDirectorySort.Attention => query
            .OrderBy(t => t.Status == TenantStatus.Suspended ? 0
                : t.Status == TenantStatus.Provisioned ? 1
                : t.Status == TenantStatus.Installing ? 2
                : t.Status == TenantStatus.Active ? 3
                : 4)
            .ThenByDescending(t => t.UpdatedAt)
            .ThenByDescending(t => t.Id),
        _ => query.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id)
    };

    /// <summary>
    /// Predicado de keyset — mesma forma de <c>GetAuditLogQueryHandler</c>
    /// (<c>campo &lt; cursor.Campo || (== &amp;&amp; Id &lt; cursor.Id)</c>), um ramo por critério de
    /// ordenação porque cada um usa um tipo de valor primário diferente. Cursor malformado/de outro
    /// <c>sort</c> já virou <c>null</c> em <see cref="TenantDirectoryCursor.Decode"/> — aqui só resta
    /// interpretar o valor (comparações em cima de <c>DateTimeOffset</c>/string reconstituídos, nunca
    /// de "ticks" cru, para evitar tradução de membros não suportados pelo provider Npgsql).
    /// </summary>
    private static IQueryable<Tenant> ApplyCursor(IQueryable<Tenant> query, TenantDirectorySort sort, TenantDirectoryCursorValue? cursor)
    {
        if (cursor is null)
        {
            return query;
        }

        switch (sort)
        {
            case TenantDirectorySort.Name:
                return query.Where(t =>
                    t.Name.CompareTo(cursor.Primary) > 0 ||
                    (t.Name == cursor.Primary && t.Id.CompareTo(cursor.Id) > 0));

            case TenantDirectorySort.CreatedAt:
                {
                    if (!TryParseTicks(cursor.Primary, out var createdAtCursor))
                        return query;

                    return query.Where(t =>
                        t.CreatedAt < createdAtCursor ||
                        (t.CreatedAt == createdAtCursor && t.Id.CompareTo(cursor.Id) < 0));
                }

            case TenantDirectorySort.UpdatedAt:
                {
                    if (!TryParseTicks(cursor.Primary, out var updatedAtCursor))
                        return query;

                    return query.Where(t =>
                        t.UpdatedAt < updatedAtCursor ||
                        (t.UpdatedAt == updatedAtCursor && t.Id.CompareTo(cursor.Id) < 0));
                }

            case TenantDirectorySort.Attention:
                {
                    if (!TryParseAttentionPrimary(cursor.Primary, out var rankCursor, out var updatedAtCursor))
                        return query;

                    return query.Where(t =>
                        (t.Status == TenantStatus.Suspended ? 0
                            : t.Status == TenantStatus.Provisioned ? 1
                            : t.Status == TenantStatus.Installing ? 2
                            : t.Status == TenantStatus.Active ? 3
                            : 4) > rankCursor ||
                        ((t.Status == TenantStatus.Suspended ? 0
                            : t.Status == TenantStatus.Provisioned ? 1
                            : t.Status == TenantStatus.Installing ? 2
                            : t.Status == TenantStatus.Active ? 3
                            : 4) == rankCursor &&
                         (t.UpdatedAt < updatedAtCursor || (t.UpdatedAt == updatedAtCursor && t.Id.CompareTo(cursor.Id) < 0))));
                }

            default:
                return query;
        }
    }

    /// <summary>Valor primário do cursor para a última linha retornada — espelha exatamente a chave usada por <see cref="ApplyOrder"/>/<see cref="ApplyCursor"/> para o mesmo <paramref name="sort"/>.</summary>
    private static string BuildPrimary(TenantDirectorySort sort, Tenant tenant) => sort switch
    {
        TenantDirectorySort.Name => tenant.Name,
        TenantDirectorySort.CreatedAt => tenant.CreatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture),
        TenantDirectorySort.UpdatedAt => tenant.UpdatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture),
        TenantDirectorySort.Attention => $"{TenantAttentionRanking.RankOf(tenant.Status)}:{tenant.UpdatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}",
        _ => tenant.UpdatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)
    };

    private static bool TryParseTicks(string primary, out DateTimeOffset value)
    {
        if (long.TryParse(primary, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
        {
            value = new DateTimeOffset(ticks, TimeSpan.Zero);
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryParseAttentionPrimary(string primary, out int rank, out DateTimeOffset updatedAt)
    {
        var parts = primary.Split(':', 2);
        if (parts.Length == 2 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out rank) &&
            long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
        {
            updatedAt = new DateTimeOffset(ticks, TimeSpan.Zero);
            return true;
        }

        rank = default;
        updatedAt = default;
        return false;
    }

    /// <summary>
    /// Agrega <c>storesCount</c>/<c>installationsCount</c>/<c>health</c> para um conjunto já
    /// decidido de tenants (a página, ou um lote candidato do filtro de saúde) — um
    /// <c>SetTenantContextAsync</c> por tenant, mesmo padrão de <c>GetPlatformSummaryQueryHandler</c>.
    /// </summary>
    private async Task<List<TenantDirectoryRow>> BuildRowsAsync(IReadOnlyList<Tenant> tenants, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rows = new List<TenantDirectoryRow>(tenants.Count);

        foreach (var tenant in tenants)
        {
            await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

            var storesCount = await _db.Stores.AsNoTracking()
                .CountAsync(s => s.TenantId == tenant.Id && s.DeletedAt == null, cancellationToken);

            var installedLastSeenAtValues = await _db.EdgeInstallations.AsNoTracking()
                .Where(i => i.TenantId == tenant.Id && i.InstalledAt != null)
                .Select(i => i.LastSeenAt)
                .ToListAsync(cancellationToken);

            var health = TenantHealthClassifier.Classify(now, installedLastSeenAtValues);

            rows.Add(new TenantDirectoryRow(tenant, storesCount, installedLastSeenAtValues.Count, health));
        }

        return rows;
    }

    /// <summary>
    /// Caminho usado só quando <c>health</c> é filtrado (ver docstring de
    /// <see cref="MaxHealthFilterBatches"/>) — avança em lotes pela MESMA ordenação/cursor da tabela
    /// <c>tenant</c>, agrega saúde por lote e descarta quem não bate, até formar <c>Limit + 1</c>
    /// linhas (para saber <c>hasMore</c>) ou esgotar o teto de lotes/candidatos.
    /// </summary>
    private async Task<(List<TenantDirectoryRow> Rows, bool HasMore)> FetchWithHealthFilterAsync(
        ListTenantsQuery request,
        string? normalizedSearch,
        TenantDirectoryCursorValue? initialCursor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = new List<TenantDirectoryRow>();
        var cursor = initialCursor;
        var target = request.Limit + 1;

        for (var batch = 0; batch < MaxHealthFilterBatches && rows.Count < target; batch++)
        {
            var query = ApplyOrder(ApplyCursor(BuildBaseQuery(request, normalizedSearch), request.Sort, cursor), request.Sort);
            var candidates = await query.AsNoTracking().Take(HealthFilterBatchSize).ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                break;
            }

            var candidateRows = await BuildRowsAsync(candidates, now, cancellationToken);
            foreach (var row in candidateRows)
            {
                if (!request.HealthStatuses.Contains(row.Health))
                {
                    continue;
                }

                rows.Add(row);
                if (rows.Count >= target)
                {
                    break;
                }
            }

            var last = candidates[^1];
            cursor = new TenantDirectoryCursorValue(BuildPrimary(request.Sort, last), last.Id);

            if (candidates.Count < HealthFilterBatchSize)
            {
                break; // esgotou a base filtrada — não há mais candidatos a examinar
            }
        }

        var hasMore = rows.Count > request.Limit;
        return (hasMore ? rows.Take(request.Limit).ToList() : rows, hasMore);
    }

    private sealed record TenantDirectoryRow(Tenant Tenant, int StoresCount, int InstallationsCount, TenantHealthStatus Health);
}
