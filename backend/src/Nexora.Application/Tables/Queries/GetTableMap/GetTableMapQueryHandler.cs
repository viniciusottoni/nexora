using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Tables;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Queries.GetTableMap;

/// <summary>
/// Monta o mapa de mesas inteiro (US-023) numa única passada por mesa/sessão/item — sem N+1: são
/// no máximo 4 idas ao banco (mesas, sessões abertas, itens das sessões abertas, garçons),
/// independente do número de mesas, o que é o que garante a meta de desempenho do §12 ("60 mesas
/// em menos de 1 s", medido aqui como tempo de execução da query — ver
/// <c>TableMapPerformanceIntegrationTests</c> e a nota de limitação lá).
/// </summary>
internal sealed class GetTableMapQueryHandler : IRequestHandler<GetTableMapQuery, Result<TableMapResponse>>
{
    // Janela para o cálculo de tempo médio de permanência quando metric_daily.avg_stay_seconds
    // (ADR-012) ainda não foi populado por nenhum worker — ver BuildAverageDurationByStoreAsync.
    private static readonly TimeSpan AverageDurationLookback = TimeSpan.FromDays(30);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetTableMapQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<TableMapResponse>> Handle(GetTableMapQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<TableMapResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        var now = DateTimeOffset.UtcNow;

        // RLS (interceptor de conexão) já restringe ao tenant corrente — sem filtro manual de
        // tenant_id aqui (CLAUDE.md, ADR-004).
        var tables = await _db.DiningTables
            .AsNoTracking()
            .Include(t => t.Area)
            .Where(t => t.IsActive && t.DeletedAt == null)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Label)
            .ToListAsync(cancellationToken);

        if (tables.Count == 0)
        {
            return Result<TableMapResponse>.Success(new TableMapResponse(Array.Empty<TableMapEntryResponse>()));
        }

        var tableIds = tables.Select(t => t.Id).ToList();
        var storeIds = tables.Select(t => t.StoreId).Distinct().ToList();

        // "Sessão corrente" = a mais recente ainda não liberada (release() só acontece no
        // fechamento efetivo da mesa) — pode estar Open, BillRequested ou Paid aguardando
        // fechamento formal, mas enquanto ReleasedAt for nulo ela ainda ocupa a mesa no salão.
        var openSessions = await _db.TableSessions
            .AsNoTracking()
            .Where(s => tableIds.Contains(s.TableId) && s.ReleasedAt == null)
            .ToListAsync(cancellationToken);

        var sessionByTable = openSessions
            .GroupBy(s => s.TableId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.OpenedAt).First());

        var sessionIds = sessionByTable.Values.Select(s => s.Id).ToList();

        var itemAggregates = await BuildItemAggregatesBySessionAsync(sessionIds, cancellationToken);
        var waiterNames = await BuildWaiterNamesAsync(sessionByTable.Values, cancellationToken);
        var avgDurationSecondsByStore = await BuildAverageDurationByStoreAsync(storeIds, now, cancellationToken);
        var waiterCalledSessionIds = await BuildWaiterCalledSessionIdsAsync(sessionIds, cancellationToken);

        var entries = new List<TableMapEntryResponse>(tables.Count);

        foreach (var table in tables)
        {
            sessionByTable.TryGetValue(table.Id, out var session);

            if (request.MineOnly)
            {
                // Mesa livre não tem sessão (logo não tem garçom) — nunca aparece com o filtro
                // "minhas mesas" ligado (Gherkin "Filtro por responsabilidade").
                if (session?.WaiterId is not { } sessionWaiterId || sessionWaiterId != _tenantContext.UserId)
                {
                    continue;
                }
            }

            entries.Add(BuildEntry(table, session, itemAggregates, waiterNames, avgDurationSecondsByStore, waiterCalledSessionIds, now));
        }

        var ordered = request.SortBy == TableMapSortBy.Urgency
            ? entries
                .OrderByDescending(UrgencyScore)
                .ThenByDescending(e => e.Session?.MinutesOpen ?? -1)
                .ThenBy(e => e.Label, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : entries; // já ordenado por dining_table.sort_order/label (o "número da mesa" do §10)

        return Result<TableMapResponse>.Success(new TableMapResponse(ordered));
    }

    private static TableMapEntryResponse BuildEntry(
        DiningTable table,
        TableSession? session,
        IReadOnlyDictionary<Guid, (decimal Total, int ReadyCount)> itemAggregates,
        IReadOnlyDictionary<Guid, string> waiterNames,
        IReadOnlyDictionary<Guid, double> avgDurationSecondsByStore,
        IReadOnlySet<Guid> waiterCalledSessionIds,
        DateTimeOffset now)
    {
        if (session is null)
        {
            var freeStatus = table.Status switch
            {
                TableStatus.Free => "FREE",
                TableStatus.Reserved => "RESERVED",
                TableStatus.Blocked => "BLOCKED",
                // TableStatus.Occupied sem sessão ativa é inconsistência de dado (não deveria
                // acontecer em operação normal) — nunca reportamos "livre" nesse caso, o garçom
                // seria induzido a sentar clientes numa mesa possivelmente já em uso.
                _ => "OCCUPIED"
            };

            return new TableMapEntryResponse(
                table.Id,
                table.Label,
                table.Area.Name,
                freeStatus,
                table.Seats,
                Session: null,
                Flags: new TableMapFlagsResponse(WaiterCalled: false, BillRequested: false, ItemsReadyToServe: 0, AboveAvgDuration: false));
        }

        var (total, readyCount) = itemAggregates.TryGetValue(session.Id, out var aggregate) ? aggregate : (0m, 0);
        var waiterResponse = session.WaiterId is { } waiterId && waiterNames.TryGetValue(waiterId, out var waiterName)
            ? new TableMapWaiterResponse(waiterId, waiterName)
            : null;

        var minutesOpen = (int)Math.Max(0, (now - session.OpenedAt).TotalMinutes);
        var sessionResponse = new TableMapSessionResponse(session.OpenedAt, minutesOpen, total, session.GuestCount, waiterResponse, session.Id);

        var billRequested = session.Status is TableSessionStatus.BillRequested;
        var aboveAvgDuration = avgDurationSecondsByStore.TryGetValue(table.StoreId, out var avgSeconds)
            && (now - session.OpenedAt).TotalSeconds > avgSeconds;

        var flags = new TableMapFlagsResponse(
            // US-025: reflete um Alert WAITER_CALLED não resolvido para a sessão CORRENTE da mesa
            // (ver BuildWaiterCalledSessionIdsAsync) — some do mapa assim que o garçom confirma o
            // atendimento (Alert.Resolve, AcknowledgeWaiterCallCommandHandler).
            WaiterCalled: waiterCalledSessionIds.Contains(session.Id),
            BillRequested: billRequested,
            ItemsReadyToServe: readyCount,
            AboveAvgDuration: aboveAvgDuration);

        return new TableMapEntryResponse(
            table.Id,
            table.Label,
            table.Area.Name,
            billRequested ? "BILL_REQUESTED" : "OCCUPIED",
            table.Seats,
            sessionResponse,
            flags);
    }

    /// <summary>Urgência = quantos sinais de ação pendente a mesa carrega, do mais para o menos crítico (US-023 §10/§4 "Ação pendente destacada").</summary>
    private static int UrgencyScore(TableMapEntryResponse entry) =>
        (entry.Flags.BillRequested ? 8 : 0) +
        (entry.Flags.WaiterCalled ? 4 : 0) +
        (entry.Flags.ItemsReadyToServe > 0 ? 2 : 0) +
        (entry.Flags.AboveAvgDuration ? 1 : 0);

    /// <summary>
    /// Soma o valor consumido (itens não cancelados) e conta itens prontos, por sessão — direto
    /// de <c>order_item</c>, nunca do campo <c>table_session.total_amount</c> (que só é
    /// preenchido em <see cref="TableSession.MarkAsPaid"/>, no fechamento da conta). US-023 §12
    /// exige que o valor exibido bata com a soma dos itens EM TEMPO REAL, sessão ainda aberta.
    /// </summary>
    private async Task<Dictionary<Guid, (decimal Total, int ReadyCount)>> BuildItemAggregatesBySessionAsync(
        IReadOnlyList<Guid> sessionIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, (decimal Total, int ReadyCount)>();
        if (sessionIds.Count == 0)
        {
            return result;
        }

        var rows = await _db.Orders
            .AsNoTracking()
            .Where(o => o.SessionId != null && sessionIds.Contains(o.SessionId!.Value))
            .SelectMany(o => o.Items, (o, item) => new { o.SessionId, item.Status, item.TotalPrice })
            .ToListAsync(cancellationToken);

        foreach (var group in rows.GroupBy(r => r.SessionId!.Value))
        {
            var total = group.Where(r => r.Status != OrderItemStatus.Cancelled).Sum(r => r.TotalPrice);
            var readyCount = group.Count(r => r.Status == OrderItemStatus.Ready);
            result[group.Key] = (total, readyCount);
        }

        return result;
    }

    /// <summary>US-025: sessões com um <c>Alert</c> tipo <see cref="Domain.Metrics.AlertTypes.WaiterCalled"/> ainda não resolvido — ver docstring de <see cref="TableMapFlagsResponse.WaiterCalled"/>.</summary>
    private async Task<IReadOnlySet<Guid>> BuildWaiterCalledSessionIdsAsync(
        IReadOnlyList<Guid> sessionIds, CancellationToken cancellationToken)
    {
        if (sessionIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var entityIds = await _db.Alerts
            .AsNoTracking()
            .Where(a => a.Type == Domain.Metrics.AlertTypes.WaiterCalled
                        && a.ResolvedAt == null
                        && a.EntityType == "table_session"
                        && a.EntityId != null
                        && sessionIds.Contains(a.EntityId!.Value))
            .Select(a => a.EntityId!.Value)
            .ToListAsync(cancellationToken);

        return entityIds.ToHashSet();
    }

    private async Task<Dictionary<Guid, string>> BuildWaiterNamesAsync(
        IEnumerable<TableSession> sessions, CancellationToken cancellationToken)
    {
        var waiterIds = sessions.Where(s => s.WaiterId is not null).Select(s => s.WaiterId!.Value).Distinct().ToList();
        if (waiterIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _db.Users
            .AsNoTracking()
            .Where(u => waiterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);
    }

    /// <summary>
    /// Tempo médio de permanência por loja, usado para <see cref="TableMapFlagsResponse.AboveAvgDuration"/>.
    /// </summary>
    /// <remarks>
    /// DECISÃO (US-023 §8, campo <c>metric_daily.avg_session_seconds</c>): o agregado oficial do
    /// ADR-012 é <c>MetricDaily.AvgStaySeconds</c>, mantido pelo <c>MetricAggregationWorker</c> —
    /// que ainda não existe nesta solution (nenhuma história anterior o implementou). Em vez de
    /// bloquear esta US numa dependência que não é dela, a função primeiro tenta ler
    /// <c>MetricDaily</c> dos últimos 30 dias; para toda loja SEM nenhuma linha populada, cai para
    /// uma média direta sobre as sessões já FECHADAS no mesmo período (mesma conta que o worker
    /// noturno faria, calculada sob demanda em vez de precalculada). Quando o worker existir, o
    /// fallback simplesmente para de disparar — nenhuma mudança de código necessária aqui.
    /// </remarks>
    private async Task<Dictionary<Guid, double>> BuildAverageDurationByStoreAsync(
        IReadOnlyList<Guid> storeIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, double>();
        var cutoffDay = DateOnly.FromDateTime((now - AverageDurationLookback).UtcDateTime);

        var metricRows = await _db.MetricDailies
            .AsNoTracking()
            .Where(m => storeIds.Contains(m.StoreId) && m.BusinessDay >= cutoffDay && m.AvgStaySeconds != null)
            .Select(m => new { m.StoreId, Seconds = m.AvgStaySeconds!.Value })
            .ToListAsync(cancellationToken);

        foreach (var group in metricRows.GroupBy(m => m.StoreId))
        {
            result[group.Key] = group.Average(m => m.Seconds);
        }

        var storesWithoutMetric = storeIds.Where(id => !result.ContainsKey(id)).ToList();
        if (storesWithoutMetric.Count == 0)
        {
            return result;
        }

        var cutoffTimestamp = now - AverageDurationLookback;
        var closedSessions = await _db.TableSessions
            .AsNoTracking()
            .Where(s => storesWithoutMetric.Contains(s.StoreId) && s.ClosedAt != null && s.OpenedAt >= cutoffTimestamp)
            .Select(s => new { s.StoreId, s.OpenedAt, ClosedAt = s.ClosedAt!.Value })
            .ToListAsync(cancellationToken);

        foreach (var group in closedSessions.GroupBy(s => s.StoreId))
        {
            result[group.Key] = group.Average(s => (s.ClosedAt - s.OpenedAt).TotalSeconds);
        }

        return result;
    }
}
