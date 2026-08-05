using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Availability;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Queries.GetKdsHistory;

/// <summary>
/// US-046 (Histórico do turno no KDS) — resolve a fila da praça de itens já SERVIDOS dentro do dia
/// operacional corrente, com busca opcional por código curto/mesa e o resumo do turno (contagem +
/// tempo médio de produção).
///
/// [DECISÃO] A delimitação pelo turno reaproveita <see cref="Order.BusinessDay"/> — já materializado
/// em <c>CreateOrderCommandHandler</c> com a mesma <see cref="BusinessDayPolicy"/> usada aqui — em
/// vez de recalcular o dia operacional a partir dos carimbos do ITEM (ex.: <c>ServedAt</c>). Um
/// pedido pertence a um único turno, fixado no momento em que nasceu; usar o campo já persistido
/// evita duas fórmulas concorrentes para a mesma pergunta ("de qual turno é este pedido?") e é
/// coerente com o cenário Gherkin "Delimitação pelo dia operacional" (item concluído às 00h40, virada
/// às 5h — o PEDIDO nasceu antes da virada, então <c>BusinessDay</c> já é o do turno corrente).
///
/// [DECISÃO] Busca (<see cref="GetKdsHistoryQuery.Search"/>) é aplicada em memória, depois de
/// materializar a lista — mesma premissa de <c>GetKdsQueueQueryHandler</c> (volume de um turno de
/// UMA praça é pequeno, dezenas a poucas centenas de itens), e evita depender de
/// <c>EF.Functions.ILike</c> (específico do provider Npgsql) só para obter
/// case-insensitive — <c>string.Contains(string, StringComparison.OrdinalIgnoreCase)</c> já resolve
/// isso de forma portátil uma vez fora da árvore de expressão traduzida para SQL.
/// </summary>
internal sealed class GetKdsHistoryQueryHandler : IRequestHandler<GetKdsHistoryQuery, Result<GetKdsHistoryResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetKdsHistoryQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<GetKdsHistoryResponse>> Handle(GetKdsHistoryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            return Result<GetKdsHistoryResponse>.Failure("Contexto de loja não identificado.", ApiErrorCodes.TenantContextMissing);
        }

        var stationExists = await _db.Stations
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.StationId && s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

        if (!stationExists)
        {
            return Result<GetKdsHistoryResponse>.Failure("Praça não encontrada.", ApiErrorCodes.StationNotFound);
        }

        var tenantConfig = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var startHourUtc = BusinessDayPolicy.ResolveStartHourUtc(tenantConfig?.Operation);
        var currentBusinessDay = DateOnly.FromDateTime(
            BusinessDayPolicy.CurrentBusinessDayStart(DateTimeOffset.UtcNow, startHourUtc).UtcDateTime);

        var items = await _db.OrderItems
            .AsNoTracking()
            .Where(i => i.StationId == request.StationId
                && i.Status == OrderItemStatus.Served
                && i.Order.BusinessDay == currentBusinessDay)
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Order).ThenInclude(o => o.Session).ThenInclude(s => s!.Table)
            .OrderByDescending(i => i.ServedAt)
            .ToListAsync(cancellationToken);

        var search = request.Search;
        var filtered = string.IsNullOrWhiteSpace(search)
            ? items
            : items.Where(i =>
                    i.Order.ShortCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (i.Order.Session?.Table.Label.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

        // Mesma resolução de autor de GetOrderItemTimelineQueryHandler (US-032) — só T5 (ServedBy)
        // interessa aqui, o histórico não mostra a timeline completa, só quem serviu.
        var actorIds = filtered.Where(i => i.ServedBy is not null).Select(i => i.ServedBy!.Value).Distinct().ToList();
        var actors = actorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.AsNoTracking()
                .Where(u => actorIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var responses = filtered.Select(item =>
        {
            var prepSeconds = OrderItemDurationCalculator.Calculate(
                item.PlacedAt, item.FiredAt, item.OvenInAt, item.OvenOutAt, item.ReadyAt, item.ServedAt).PrepSeconds ?? 0;

            var operatorResponse = item.ServedBy is { } servedBy && actors.TryGetValue(servedBy, out var name)
                ? new OrderItemTimelineActorResponse(servedBy, name)
                : null;

            return new KdsHistoryItemResponse(
                item.Id,
                item.OrderId,
                item.Order.ShortCode,
                $"{item.Variant.Product.Name} {item.Variant.Name}".Trim(),
                item.Order.Session?.Table.Label,
                item.FiredAt,
                item.ReadyAt,
                // ck_item_sequence (banco) já garante ServedAt preenchido quando Status == Served.
                item.ServedAt!.Value,
                prepSeconds,
                operatorResponse);
        }).ToList();

        var count = responses.Count;
        var avgPrepSeconds = count == 0 ? 0 : (int)Math.Round(responses.Average(r => (double)r.PrepSeconds));

        return Result<GetKdsHistoryResponse>.Success(
            new GetKdsHistoryResponse(responses, new KdsHistorySummaryResponse(count, avgPrepSeconds)));
    }
}
