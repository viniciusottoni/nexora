using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Stations;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Stations.Commands.UpdateStation;

internal sealed class UpdateStationCommandHandler : IRequestHandler<UpdateStationCommand, Result<StationResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateStationCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<StationResponse>> Handle(UpdateStationCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<StationResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var station = await _db.Stations
            .FirstOrDefaultAsync(s => s.Id == request.StationId && s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

        if (station is null)
        {
            return Result<StationResponse>.Failure("Praça não encontrada.", ApiErrorCodes.StationNotFound);
        }

        var before = JsonSerializer.Serialize(new
        {
            name = station.Name,
            color = station.Color,
            capacitySlots = station.CapacitySlots,
            isBottleneck = station.IsBottleneck,
            position = station.SortOrder
        });

        var changedKeys = new List<string>();

        if (request.Name is not null && request.Name != station.Name)
            changedKeys.Add("name");
        if (request.Color is not null && request.Color != station.Color)
            changedKeys.Add("color");
        if (request.Position is not null && request.Position != station.SortOrder)
            changedKeys.Add("position");

        station.UpdateDetails(
            request.Name ?? station.Name,
            request.Color ?? station.Color,
            request.Position ?? station.SortOrder);

        if (request.CapacitySlots.HasValue && request.CapacitySlots != station.CapacitySlots)
        {
            changedKeys.Add("capacitySlots");
            station.UpdateCapacity(request.CapacitySlots, station.AvgCookSeconds);
        }

        // "Apenas uma praça pode ser marcada como gargalo por vez" (US-017 §10) — desmarca
        // qualquer outra praça do mesmo tenant/loja na MESMA transação antes de marcar esta.
        if (request.IsBottleneck == true && !station.IsBottleneck)
        {
            changedKeys.Add("isBottleneck");
            await UnmarkOtherBottlenecksAsync(tenantId, station.StoreId, station.Id, cancellationToken);
            station.MarkAsBottleneck();
        }
        else if (request.IsBottleneck == false && station.IsBottleneck)
        {
            changedKeys.Add("isBottleneck");
            station.UnmarkAsBottleneck();
        }

        if (changedKeys.Count > 0)
        {
            var after = JsonSerializer.Serialize(new
            {
                name = station.Name,
                color = station.Color,
                capacitySlots = station.CapacitySlots,
                isBottleneck = station.IsBottleneck,
                position = station.SortOrder
            });

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "STATION_UPDATED",
                entity: "station",
                occurredAt: now,
                storeId: station.StoreId,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: station.Id,
                before: before,
                after: after));

            // EVT-054 tenant.config_updated (US-017 §6).
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "tenant.config_updated",
                aggregateType: "station",
                aggregateId: station.Id,
                payload: JsonSerializer.Serialize(new { stationId = station.Id, changedKeys }),
                origin: "CLOUD",
                occurredAt: now,
                storeId: station.StoreId,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        var linkedProductCount = await _db.Products
            .CountAsync(p => p.StationId == station.Id && p.DeletedAt == null, cancellationToken);

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<StationResponse>.Success(new StationResponse(
            station.Id,
            station.Code,
            station.Name,
            station.Color,
            station.CapacitySlots,
            station.IsBottleneck,
            station.SortOrder,
            station.IsActive,
            linkedProductCount));
    }

    private async Task UnmarkOtherBottlenecksAsync(Guid tenantId, Guid storeId, Guid keepStationId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.Stations
            .Where(s => s.TenantId == tenantId && s.StoreId == storeId && s.IsBottleneck && s.DeletedAt == null && s.Id != keepStationId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.IsBottleneck, false)
                    .SetProperty(s => s.UpdatedAt, now),
                cancellationToken);

        // ExecuteUpdate bypasses EF's change tracker. Synchronize loaded aggregates as well,
        // otherwise a following operation in the same request scope can read stale state.
        foreach (var trackedStation in _db.Stations.Local.Where(s =>
                     s.TenantId == tenantId && s.StoreId == storeId && s.Id != keepStationId
                     && s.IsBottleneck && s.DeletedAt == null))
        {
            trackedStation.UnmarkAsBottleneck();
        }
    }
}
