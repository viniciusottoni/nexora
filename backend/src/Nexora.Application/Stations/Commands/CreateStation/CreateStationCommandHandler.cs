using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Stations;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Stations.Commands.CreateStation;

internal sealed class CreateStationCommandHandler : IRequestHandler<CreateStationCommand, Result<StationResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public CreateStationCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<StationResponse>> Handle(CreateStationCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<StationResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (_tenantContext.StoreId is null)
        {
            return Result<StationResponse>.Failure(
                "Loja não definida para esta requisição.",
                ApiErrorCodes.StationStoreContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var storeId = _tenantContext.StoreId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var codeTaken = await _db.Stations
            .AsNoTracking()
            .AnyAsync(s => s.TenantId == tenantId && s.Code == request.Code && s.DeletedAt == null, cancellationToken);

        if (codeTaken)
        {
            return Result<StationResponse>.Failure("Já existe uma praça com este código.", ApiErrorCodes.StationCodeAlreadyExists);
        }

        // "Apenas uma praça pode ser marcada como gargalo por vez" (US-017 §10) — ao criar uma nova
        // praça já marcada como gargalo, desmarca qualquer outra na mesma transação.
        if (request.IsBottleneck)
        {
            await UnmarkOtherBottlenecksAsync(tenantId, storeId, keepStationId: null, cancellationToken);
        }

        var station = Station.Create(
            tenantId,
            storeId,
            request.Code,
            request.Name,
            type: StationType.Other,
            sortOrder: request.Position,
            color: request.Color,
            isBottleneck: request.IsBottleneck);

        if (request.CapacitySlots.HasValue)
        {
            station.UpdateCapacity(request.CapacitySlots, avgCookSeconds: null);
        }

        _db.Stations.Add(station);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "STATION_CREATED",
            entity: "station",
            occurredAt: now,
            storeId: storeId,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: station.Id,
            after: JsonSerializer.Serialize(new
            {
                code = station.Code,
                name = station.Name,
                color = station.Color,
                capacitySlots = station.CapacitySlots,
                isBottleneck = station.IsBottleneck,
                position = station.SortOrder
            })));

        // EVT-054 tenant.config_updated — praça criada (US-017 §6).
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "station",
            aggregateId: station.Id,
            payload: JsonSerializer.Serialize(new { stationId = station.Id, changedKeys = new[] { "created" } }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: storeId,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId));

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
            LinkedProductCount: 0));
    }

    private async Task UnmarkOtherBottlenecksAsync(Guid tenantId, Guid storeId, Guid? keepStationId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.Stations
            .Where(s => s.TenantId == tenantId && s.StoreId == storeId && s.IsBottleneck && s.DeletedAt == null
                        && (keepStationId == null || s.Id != keepStationId.Value))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.IsBottleneck, false)
                    .SetProperty(s => s.UpdatedAt, now),
                cancellationToken);

        // ExecuteUpdate bypasses EF's change tracker. Keep already-loaded stations consistent
        // with the database so later commands/queries in the same unit of work cannot observe
        // the previous bottleneck as still active.
        foreach (var trackedStation in _db.Stations.Local.Where(s =>
                     s.TenantId == tenantId && s.StoreId == storeId && s.IsBottleneck && s.DeletedAt == null
                     && (keepStationId == null || s.Id != keepStationId.Value)))
        {
            trackedStation.UnmarkAsBottleneck();
        }
    }
}
