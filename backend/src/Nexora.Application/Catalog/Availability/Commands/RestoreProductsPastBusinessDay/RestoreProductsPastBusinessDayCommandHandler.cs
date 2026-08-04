using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Alerts.Support;
using Nexora.Domain.Metrics;
using Nexora.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Availability.Commands.RestoreProductsPastBusinessDay;

internal sealed class RestoreProductsPastBusinessDayCommandHandler
    : IRequestHandler<RestoreProductsPastBusinessDayCommand, Result<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IAvailabilityBroadcaster _broadcaster;
    private readonly IAlertRaiser _alertRaiser;

    public RestoreProductsPastBusinessDayCommandHandler(
        IApplicationDbContext db, IEventOriginProvider eventOrigin, IAvailabilityBroadcaster broadcaster, IAlertRaiser alertRaiser)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
        _alertRaiser = alertRaiser;
    }

    public async Task<Result<int>> Handle(RestoreProductsPastBusinessDayCommand request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;
        var now = DateTimeOffset.UtcNow;

        // RLS (ADR-004): este comando roda fora de uma requisição HTTP autenticada (chamado por um
        // BackgroundService) — fixa o contexto explicitamente, mesmo mecanismo de
        // LoginWithPasswordCommandHandler/ProvisionTenantCommandHandler.
        await _db.SetTenantContextAsync(tenantId, cancellationToken);

        var config = await _db.TenantConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var startHourUtc = Availability.BusinessDayPolicy.ResolveStartHourUtc(config?.Operation);

        var candidates = await _db.Products
            .Where(p => p.TenantId == tenantId && p.DeletedAt == null && !p.IsAvailable
                        && p.AutoRestoreNextDay && p.UnavailableSince != null)
            .ToListAsync(cancellationToken);

        var restoredCount = 0;

        foreach (var product in candidates)
        {
            if (!Availability.BusinessDayPolicy.HasCrossedBusinessDayBoundary(product.UnavailableSince!.Value, now, startHourUtc))
            {
                continue;
            }

            product.MarkAvailable();
            restoredCount++;

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRODUCT_AUTO_RESTORED",
                entity: "product",
                occurredAt: now,
                entityId: product.Id,
                after: JsonSerializer.Serialize(new { isAvailable = true })));

            // EVT-051 product.availability_changed — mesmo tipo de evento do retorno manual, com
            // autoRestored=true para diferenciar na auditoria/resumo diário futuro.
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "product.availability_changed",
                aggregateType: "product",
                aggregateId: product.Id,
                payload: JsonSerializer.Serialize(new { productId = product.Id, isAvailable = true, autoRestored = true }),
                origin: _eventOrigin.Origin,
                occurredAt: now));

            await _broadcaster.ProductMarkedAvailableAsync(tenantId, product.Id, cancellationToken);

            // E-08/US-080 §4 "Resolução automática" — mesmo racional do retorno manual (MarkProductAvailableCommandHandler).
            await _alertRaiser.ResolveAsync(tenantId, AlertTypes.ProductUnavailable, "product", product.Id, cancellationToken);
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<int>.Success(restoredCount);
    }
}
