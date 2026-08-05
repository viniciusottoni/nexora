using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.PrepTime;
using Nexora.Application.Orders.Support;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Queries.GetKdsQueue;

/// <summary>
/// US-031 (Roteamento simultâneo para cozinha e caixa) — fila ATIVA de uma praça (itens que ainda
/// não foram servidos/cancelados), ordenada pelo mais antigo primeiro (FIFO — quem chegou primeiro é
/// preparado primeiro, sem prioridade dinâmica: essa é US-116, Fase 2, fora de escopo aqui).
///
/// [DECISÃO DE ESCOPO — snapshot completo, não delta] Esta consulta SEMPRE devolve TODOS os itens
/// ativos da praça, ignorando <see cref="GetKdsQueueQuery.Since"/> como filtro (só o aceita no
/// contrato por simetria com o ADR-011). É a mesma decisão documentada em <c>KdsHub.Resume</c>: um
/// volume de itens ativos por praça realisticamente pequeno (dezenas) torna trivial reenviar tudo a
/// cada poll/reconexão, e isso elimina de raiz a classe de bug "pedido ausente da fila por corte de
/// janela de tempo/paginação" — exatamente o risco que a US-031 §15 aponta como "falha silenciosa".
/// <see cref="GetKdsQueueResponse.LastEventId"/> devolve um cursor (timestamp UTC serializado) só
/// para o cliente guardar e devolver na chamada seguinte — não é usado para filtrar nada hoje.
/// </summary>
internal sealed class GetKdsQueueQueryHandler : IRequestHandler<GetKdsQueueQuery, Result<GetKdsQueueResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetKdsQueueQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<GetKdsQueueResponse>> Handle(GetKdsQueueQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            return Result<GetKdsQueueResponse>.Failure("Contexto de loja não identificado.", ApiErrorCodes.TenantContextMissing);
        }

        var stationExists = await _db.Stations
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.StationId && s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

        if (!stationExists)
        {
            return Result<GetKdsQueueResponse>.Failure("Praça não encontrada.", ApiErrorCodes.StationNotFound);
        }

        var items = await _db.OrderItems
            .AsNoTracking()
            // "Ativo" = ainda não chegou a um estado final (servido/cancelado) — evita
            // array.Contains(enum) numa query LINQ-to-Entities (EF Core 10 preview: interpretador de
            // expressão falha ao capturar um static readonly OrderItemStatus[] aqui, erro em tempo de
            // execução, não de tradução SQL — comparação direta é tão clara quanto e sempre traduz).
            .Where(i => i.StationId == request.StationId && i.Status != OrderItemStatus.Served && i.Status != OrderItemStatus.Cancelled)
            .Include(i => i.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Modifiers)
            .Include(i => i.Fractions).ThenInclude(f => f.Variant).ThenInclude(v => v.Product)
            .Include(i => i.Order).ThenInclude(o => o.Session).ThenInclude(s => s!.Table)
            .OrderBy(i => i.PlacedAt)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        // US-040 §5 — limiar efetivo por item, mesma resolução (variação → padrão do tenant) de
        // GetVariantPrepTimeAnalysisQueryHandler (US-016). Um único SELECT por chamada: o volume de
        // itens ativos por praça é pequeno (mesma premissa documentada acima para o snapshot
        // completo), então buscar o tenant_config uma vez fora do loop é suficiente.
        var tenantConfig = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var (defaultWarn, defaultCritical) = TenantPrepTimeDefaults.Resolve(tenantConfig?.Thresholds);

        var responses = items.Select(item =>
        {
            var elapsedSeconds = Math.Max(0, (int)(now - item.PlacedAt).TotalSeconds);
            var warnMinutes = item.Variant.WarnMinutes ?? defaultWarn;
            var criticalMinutes = item.Variant.CriticalMinutes ?? defaultCritical;
            var elapsedMinutes = elapsedSeconds / 60.0;
            var thresholdState = elapsedMinutes >= criticalMinutes
                ? "CRITICAL"
                : elapsedMinutes >= warnMinutes
                    ? "WARNING"
                    : "NORMAL";

            return new KdsQueueItemResponse(
                item.Id,
                item.OrderId,
                item.Order.ShortCode,
                item.Variant.ProductId,
                $"{item.Variant.Product.Name} {item.Variant.Name}".Trim(),
                item.Quantity,
                item.Modifiers.Select(m => m.NameSnapshot).ToArray(),
                item.Notes,
                OrderItemStatusLabels.ToWireStatus(item.Status),
                item.PlacedAt,
                elapsedSeconds,
                thresholdState,
                warnMinutes * 60,
                criticalMinutes * 60,
                item.Order.Session?.Table.Label,
                item.Order.Channel.ToString(),
                item.Fractions
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new KdsQueueItemFractionResponse($"{f.Variant.Product.Name} {f.Variant.Name}".Trim(), f.Weight))
                    .ToList());
        }).ToList();

        return Result<GetKdsQueueResponse>.Success(new GetKdsQueueResponse(responses, now.ToString("O")));
    }
}
