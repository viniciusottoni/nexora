using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Areas.Commands.UpdateArea;

internal sealed class UpdateAreaCommandHandler : IRequestHandler<UpdateAreaCommand, Result<AreaResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateAreaCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AreaResponse>> Handle(UpdateAreaCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<AreaResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        // TenantId redundante ao RLS (ADR-004 já barra a linha de outro tenant) — mesma
        // convenção defensiva de UpdateRoleCommandHandler: uma área de outro tenant não existe
        // para esta consulta, o que produz 404 (nunca 403) por construção (ADR-021).
        var area = await _db.Areas.SingleOrDefaultAsync(
            a => a.Id == request.Id && a.TenantId == tenantId && a.DeletedAt == null, cancellationToken);
        if (area is null)
        {
            return Result<AreaResponse>.Failure("Ambiente não encontrado.", ApiErrorCodes.AreaNotFound);
        }

        var now = DateTimeOffset.UtcNow;
        var before = JsonSerializer.Serialize(new { area.Name, area.SortOrder });

        area.Rename(request.Name.Trim(), request.Position);

        _db.AuditLogs.Add(AuditLog.Create(
            area.TenantId,
            action: "AREA_UPDATED",
            entity: "area",
            occurredAt: now,
            storeId: area.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: area.Id,
            before: before,
            after: JsonSerializer.Serialize(new { area.Name, area.SortOrder })));

        _db.DomainEvents.Add(DomainEvent.Create(
            area.TenantId,
            type: "tenant.config_updated",
            aggregateType: "area",
            aggregateId: area.Id,
            payload: JsonSerializer.Serialize(new { areaId = area.Id, tableId = (Guid?)null }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: area.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        var tableCount = await _db.DiningTables.CountAsync(
            t => t.AreaId == area.Id && t.DeletedAt == null, cancellationToken);

        return Result<AreaResponse>.Success(new AreaResponse(area.Id, area.Name, area.SortOrder, area.IsActive, tableCount));
    }
}
