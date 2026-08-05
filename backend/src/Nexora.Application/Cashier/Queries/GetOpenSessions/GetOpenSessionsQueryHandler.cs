using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Queries.GetOpenSessions;

/// <summary>
/// Painel do caixa (US-050) — segunda "visão" dos mesmos dados de
/// <c>Nexora.Application.Tables.Queries.GetTableMap.GetTableMapQueryHandler</c> (US-023), com o
/// foco do CAIXA em vez do garçom: só sessões abertas (nenhuma mesa livre entra na lista), sem
/// agrupar por ambiente, com prioridade de conta solicitada e busca por mesa/comanda. Mesmo
/// raciocínio de "poucas idas ao banco, independente do número de mesas": no máximo 3 SELECTs
/// (sessões com mesa/ambiente, pedidos+itens das sessões, garçons), nunca N+1 por mesa.
/// </summary>
internal sealed class GetOpenSessionsQueryHandler : IRequestHandler<GetOpenSessionsQuery, Result<OpenSessionsResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetOpenSessionsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<OpenSessionsResponse>> Handle(GetOpenSessionsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<OpenSessionsResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        var now = DateTimeOffset.UtcNow;

        // Mesma semântica de "sessão corrente" do mapa do garçom (GetTableMapQueryHandler): ainda
        // não liberada (ReleasedAt nulo) — pode estar Open, BillRequested ou Paid aguardando
        // fechamento formal, mas continua ocupando a mesa e precisa aparecer no painel do caixa.
        // RLS (interceptor de conexão) já restringe ao tenant corrente — sem filtro manual de
        // tenant_id aqui (CLAUDE.md, ADR-004).
        var sessions = await _db.TableSessions
            .AsNoTracking()
            .Include(s => s.Table)
            .ThenInclude(t => t.Area)
            .Where(s => s.ReleasedAt == null)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return Result<OpenSessionsResponse>.Success(new OpenSessionsResponse(
                Array.Empty<OpenSessionEntryResponse>(), new OpenSessionsSummaryResponse(0, 0m)));
        }

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var aggregates = await BuildSessionAggregatesAsync(sessionIds, cancellationToken);
        var waiterNames = await BuildWaiterNamesAsync(sessions, cancellationToken);

        var entries = sessions
            .Select(session => BuildEntry(session, aggregates, waiterNames, now))
            .ToList();

        // Totalizador do salão (§4 "Totalizador do salão aberto", Gherkin "Visão de todas as
        // comandas") reflete TODAS as sessões abertas — indicador do salão como um todo, não do
        // resultado filtrado pela busca (ver docstring de OpenSessionsSummaryResponse.TotalOpen).
        var summary = new OpenSessionsSummaryResponse(entries.Count, entries.Sum(e => e.Total));

        var filtered = string.IsNullOrWhiteSpace(request.Search)
            ? entries
            : entries.Where(e => MatchesSearch(e, request.Search!)).ToList();

        var ordered = request.SortBy == GetOpenSessionsSortBy.Table
            ? filtered.OrderBy(e => e.Table, StringComparer.OrdinalIgnoreCase).ToList()
            // Padrão "urgência" (US-050 §4, Gherkin "Prioridade de conta solicitada"): BILL_REQUESTED
            // sempre no topo, ordenado por tempo de espera decrescente; as demais, por tempo aberto
            // decrescente. Um único ThenByDescending encadeado basta: fora do grupo BILL_REQUESTED,
            // WaitingSeconds é sempre nulo (-1), então o desempate cai direto para MinutesOpen.
            : filtered
                .OrderByDescending(e => e.Status == "BILL_REQUESTED")
                .ThenByDescending(e => e.WaitingSeconds ?? -1)
                .ThenByDescending(e => e.MinutesOpen)
                .ToList();

        return Result<OpenSessionsResponse>.Success(new OpenSessionsResponse(ordered, summary));
    }

    private static bool MatchesSearch(OpenSessionEntryResponse entry, string search)
    {
        var trimmed = search.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        if (entry.Table.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return entry.OrderCode is { } code && code.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
    }

    private static OpenSessionEntryResponse BuildEntry(
        TableSession session,
        IReadOnlyDictionary<Guid, SessionAggregate> aggregates,
        IReadOnlyDictionary<Guid, string> waiterNames,
        DateTimeOffset now)
    {
        aggregates.TryGetValue(session.Id, out var aggregate);

        var waiter = session.WaiterId is { } waiterId && waiterNames.TryGetValue(waiterId, out var waiterName)
            ? new OpenSessionWaiterResponse(waiterId, waiterName)
            : null;

        var minutesOpen = (int)Math.Max(0, (now - session.OpenedAt).TotalMinutes);

        var status = session.Status switch
        {
            TableSessionStatus.Open => "OPEN",
            TableSessionStatus.BillRequested => "BILL_REQUESTED",
            TableSessionStatus.Paid => "PAID",
            // Sessão fechada mas ainda não liberada é inconsistência transitória (Release()
            // normalmente segue Close() de perto) — reportada honestamente, nunca escondida como
            // "aberta" (mesmo espírito da nota de TableStatus.Occupied sem sessão em GetTableMapQueryHandler).
            TableSessionStatus.Closed => "CLOSED",
            _ => "OPEN"
        };

        // Só "aplicável" (US-050 §7) enquanto a conta está EFETIVAMENTE aguardando: sessão paga já
        // resolveu a espera, mesmo que BillRequestedAt continue preenchido no histórico.
        int? waitingSeconds = session.Status is TableSessionStatus.BillRequested && session.BillRequestedAt is { } billRequestedAt
            ? (int)Math.Max(0, (now - billRequestedAt).TotalSeconds)
            : null;

        return new OpenSessionEntryResponse(
            session.Id,
            session.Table.Label,
            session.Table.Area.Name,
            session.OpenedAt,
            minutesOpen,
            session.GuestCount,
            waiter,
            aggregate?.Total ?? 0m,
            status,
            session.BillRequestedAt,
            waitingSeconds,
            aggregate?.PendingItems ?? 0,
            aggregate?.LatestOrderShortCode);
    }

    /// <summary>
    /// Soma o valor consumido (itens não cancelados), conta itens ainda não servidos e resolve o
    /// <c>short_code</c> do pedido mais recente — por sessão, direto de <c>order</c>/<c>order_item</c>,
    /// nunca do campo <c>table_session.total_amount</c> (só preenchido em <c>MarkAsPaid</c>, no
    /// fechamento). "Pendente" usa a MESMA definição de <c>PendingItemsClosePolicy</c> (US-035,
    /// status diferente de <see cref="OrderItemStatus.Served"/>/<see cref="OrderItemStatus.Cancelled"/>)
    /// — o caixa decide se é seguro fechar com o mesmo critério que o próprio fechamento usa.
    /// </summary>
    private async Task<Dictionary<Guid, SessionAggregate>> BuildSessionAggregatesAsync(
        IReadOnlyList<Guid> sessionIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, SessionAggregate>();
        if (sessionIds.Count == 0)
        {
            return result;
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.SessionId != null && sessionIds.Contains(o.SessionId!.Value))
            .Select(o => new
            {
                SessionId = o.SessionId!.Value,
                o.ShortCode,
                o.CreatedAt,
                Items = o.Items.Select(i => new { i.Status, i.TotalPrice })
            })
            .ToListAsync(cancellationToken);

        foreach (var group in orders.GroupBy(o => o.SessionId))
        {
            var items = group.SelectMany(o => o.Items).ToList();
            var total = items.Where(i => i.Status != OrderItemStatus.Cancelled).Sum(i => i.TotalPrice);
            var pendingItems = items.Count(i => i.Status != OrderItemStatus.Served && i.Status != OrderItemStatus.Cancelled);
            var latestOrder = group.OrderByDescending(o => o.CreatedAt).First();
            var latestOrderShortCode = string.IsNullOrEmpty(latestOrder.ShortCode) ? null : latestOrder.ShortCode;

            result[group.Key] = new SessionAggregate(total, pendingItems, latestOrderShortCode);
        }

        return result;
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

    private sealed record SessionAggregate(decimal Total, int PendingItems, string? LatestOrderShortCode);
}
