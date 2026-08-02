using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Stations.Commands.DeleteStation;

internal sealed class DeleteStationCommandHandler : IRequestHandler<DeleteStationCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public DeleteStationCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(DeleteStationCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result.Failure(
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
            return Result.Failure("Praça não encontrada.", ApiErrorCodes.StationNotFound);
        }

        // Cenário Gherkin "Exclusão de praça com produtos vinculados" (US-017 §4): a exclusão deve
        // ser recusada e exigir reatribuição dos produtos antes — nunca excluir silenciosamente
        // arrastando os produtos vinculados.
        var linkedProductCount = await _db.Products
            .CountAsync(p => p.StationId == station.Id && p.DeletedAt == null, cancellationToken);

        if (linkedProductCount > 0)
        {
            return Result.Failure(
                $"Esta praça tem {linkedProductCount} produto(s) vinculado(s). Reatribua os produtos antes de excluí-la.",
                ApiErrorCodes.StationHasLinkedProducts);
        }

        var before = JsonSerializer.Serialize(new { code = station.Code, name = station.Name });

        station.SoftDelete();

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "STATION_DELETED",
            entity: "station",
            occurredAt: now,
            storeId: station.StoreId,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: station.Id,
            before: before));

        // EVT-054 tenant.config_updated (US-017 §6).
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "station",
            aggregateId: station.Id,
            payload: JsonSerializer.Serialize(new { stationId = station.Id, changedKeys = new[] { "deleted" } }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: station.StoreId,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result.Success();
    }
}
