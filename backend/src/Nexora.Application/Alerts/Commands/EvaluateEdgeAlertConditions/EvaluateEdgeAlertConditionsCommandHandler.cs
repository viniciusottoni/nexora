using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Domain.Metrics;
using Nexora.Domain.Operation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.EvaluateEdgeAlertConditions;

internal sealed class EvaluateEdgeAlertConditionsCommandHandler : IRequestHandler<EvaluateEdgeAlertConditionsCommand, Result<int>>
{
    /// <summary>Janela de cálculo do tempo médio (US-080, "AVG_TIME_ABOVE_TARGET") — não configurável, ao contrário dos limiares em si (nenhuma US pede isso).</summary>
    private static readonly TimeSpan AvgWindow = TimeSpan.FromMinutes(60);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IAlertRaiser _raiser;

    public EvaluateEdgeAlertConditionsCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IAlertRaiser raiser)
    {
        _db = db;
        _tenantContext = tenantContext;
        _raiser = raiser;
    }

    public async Task<Result<int>> Handle(EvaluateEdgeAlertConditionsCommand request, CancellationToken cancellationToken)
    {
        // O edge tem exatamente um tenant fixo (ADR-004) — TenantId nunca é nulo aqui, mesma
        // garantia documentada em EdgeCurrentTenantContext; a checagem é só defesa em profundidade.
        if (_tenantContext.TenantId is null)
        {
            return Result<int>.Success(0);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var storeId = _tenantContext.StoreId;
        var now = DateTimeOffset.UtcNow;

        var config = await _db.TenantConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var thresholds = AlertThresholds.Parse(config?.Thresholds);

        var raisedCount = 0;
        raisedCount += await EvaluateOrderLateAsync(tenantId, now, thresholds, cancellationToken);

        if (storeId is { } sid)
        {
            raisedCount += await EvaluateAvgTimeAboveTargetAsync(tenantId, sid, now, thresholds, cancellationToken);
            raisedCount += await EvaluateCancellationsAsync(tenantId, sid, now, thresholds, cancellationToken);
        }

        raisedCount += await EvaluateDiscountsAsync(tenantId, now, thresholds, cancellationToken);

        return Result<int>.Success(raisedCount);
    }

    /// <summary>US-080 §4, cenários "Pedido atrasado"/"Resolução automática"/"Escalonamento por duração" (a subida de severidade é feita por <see cref="IAlertRaiser.RaiseAsync"/> ao reencontrar o mesmo pedido acima do limiar crítico).</summary>
    private async Task<int> EvaluateOrderLateAsync(Guid tenantId, DateTimeOffset now, AlertThresholds thresholds, CancellationToken cancellationToken)
    {
        var openOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == tenantId
                && (o.Status == OrderStatus.Placed
                    || o.Status == OrderStatus.InProduction
                    || o.Status == OrderStatus.Ready
                    || o.Status == OrderStatus.Dispatched)
                && o.PlacedAt != null)
            .ToListAsync(cancellationToken);

        var raisedCount = 0;
        var openOrderIds = new HashSet<Guid>();

        foreach (var order in openOrders)
        {
            openOrderIds.Add(order.Id);
            var lateMinutes = (int)(now - order.PlacedAt!.Value).TotalMinutes;

            if (lateMinutes >= thresholds.OrderCriticalMinutes)
            {
                await _raiser.RaiseAsync(new RaiseAlertRequest(
                    tenantId, order.StoreId, AlertTypes.OrderLate, AlertSeverity.High,
                    $"Pedido {order.ShortCode} está há {lateMinutes} minutos sem ser entregue.",
                    "order", order.Id), cancellationToken);
                raisedCount++;
            }
            else if (lateMinutes >= thresholds.OrderWarnMinutes)
            {
                await _raiser.RaiseAsync(new RaiseAlertRequest(
                    tenantId, order.StoreId, AlertTypes.OrderLate, AlertSeverity.Warning,
                    $"Pedido {order.ShortCode} está há {lateMinutes} minutos sem ser entregue.",
                    "order", order.Id), cancellationToken);
                raisedCount++;
            }
            else
            {
                await _raiser.ResolveAsync(tenantId, AlertTypes.OrderLate, "order", order.Id, cancellationToken);
            }
        }

        // Pedido que saiu do conjunto aberto desde a última varredura (entregue/fechado/cancelado)
        // não aparece em openOrders — o alerta dele precisa ser encerrado aqui.
        var openAlerts = await _db.Alerts.Where(
            a => a.TenantId == tenantId && a.Type == AlertTypes.OrderLate && a.ResolvedAt == null && a.EntityId != null)
            .Select(a => a.EntityId!.Value)
            .ToListAsync(cancellationToken);

        foreach (var entityId in openAlerts.Where(id => !openOrderIds.Contains(id)))
        {
            await _raiser.ResolveAsync(tenantId, AlertTypes.OrderLate, "order", entityId, cancellationToken);
        }

        return raisedCount;
    }

    /// <summary>US-080 §2 "tempo médio acima da meta" — meta é a promessa de canal (dine-in/delivery), alerta é por loja, não por pedido.</summary>
    private async Task<int> EvaluateAvgTimeAboveTargetAsync(
        Guid tenantId, Guid storeId, DateTimeOffset now, AlertThresholds thresholds, CancellationToken cancellationToken)
    {
        var windowStart = now - AvgWindow;

        var finished = await _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.StoreId == storeId && o.PlacedAt != null
                        && o.ServedAt != null && o.ServedAt >= windowStart)
            .Select(o => new { o.PlacedAt, o.ServedAt })
            .ToListAsync(cancellationToken);

        if (finished.Count == 0)
        {
            await _raiser.ResolveAsync(tenantId, AlertTypes.AvgTimeAboveTarget, "store", storeId, cancellationToken);
            return 0;
        }

        var avgMinutes = finished.Average(o => (o.ServedAt!.Value - o.PlacedAt!.Value).TotalMinutes);
        var targetMinutes = thresholds.DineInPromiseMinutes;
        var limitMinutes = targetMinutes * (1 + (double)thresholds.AvgTimeAboveTargetPercent / 100);

        if (avgMinutes > limitMinutes)
        {
            await _raiser.RaiseAsync(new RaiseAlertRequest(
                tenantId, storeId, AlertTypes.AvgTimeAboveTarget, AlertSeverity.Warning,
                $"Tempo médio de atendimento está em {avgMinutes:F0} min, acima da meta de {targetMinutes} min.",
                "store", storeId), cancellationToken);
            return 1;
        }

        await _raiser.ResolveAsync(tenantId, AlertTypes.AvgTimeAboveTarget, "store", storeId, cancellationToken);
        return 0;
    }

    /// <summary>US-080 §2 "cancelamento... acima do padrão" — padrão de rajada (N cancelamentos numa janela), não um cancelamento isolado.</summary>
    private async Task<int> EvaluateCancellationsAsync(
        Guid tenantId, Guid storeId, DateTimeOffset now, AlertThresholds thresholds, CancellationToken cancellationToken)
    {
        var windowStart = now.AddMinutes(-thresholds.CancellationWindowMinutes);

        var count = await _db.Orders.AsNoTracking().CountAsync(
            o => o.TenantId == tenantId && o.StoreId == storeId && o.Status == OrderStatus.Cancelled
                 && o.CancelledAt != null && o.CancelledAt >= windowStart,
            cancellationToken);

        if (count >= thresholds.CancellationCountThreshold)
        {
            await _raiser.RaiseAsync(new RaiseAlertRequest(
                tenantId, storeId, AlertTypes.CancellationAboveThreshold, AlertSeverity.Warning,
                $"{count} pedidos cancelados nos últimos {thresholds.CancellationWindowMinutes} minutos, acima do padrão.",
                "store", storeId), cancellationToken);
            return 1;
        }

        await _raiser.ResolveAsync(tenantId, AlertTypes.CancellationAboveThreshold, "store", storeId, cancellationToken);
        return 0;
    }

    /// <summary>US-080 §2 "desconto... acima do padrão" — por pedido (um desconto individual já alto é alerta-relevante por si, ao contrário de cancelamento).</summary>
    private async Task<int> EvaluateDiscountsAsync(Guid tenantId, DateTimeOffset now, AlertThresholds thresholds, CancellationToken cancellationToken)
    {
        var windowStart = now.AddMinutes(-thresholds.DiscountWindowMinutes);
        var minPercent = thresholds.DiscountAboveThresholdPercent;

        var candidates = await _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.Subtotal > 0 && o.DiscountAmount > 0 && o.UpdatedAt >= windowStart)
            .ToListAsync(cancellationToken);

        var raisedCount = 0;
        foreach (var order in candidates)
        {
            var percent = order.DiscountAmount / order.Subtotal * 100;
            if (percent < minPercent)
            {
                continue;
            }

            await _raiser.RaiseAsync(new RaiseAlertRequest(
                tenantId, order.StoreId, AlertTypes.DiscountAboveThreshold, AlertSeverity.Warning,
                $"Pedido {order.ShortCode} recebeu desconto de {percent:F0}%, acima do padrão.",
                "order", order.Id), cancellationToken);
            raisedCount++;
        }

        return raisedCount;
    }
}
