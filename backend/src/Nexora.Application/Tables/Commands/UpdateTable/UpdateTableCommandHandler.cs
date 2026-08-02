using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.UpdateTable;

internal sealed class UpdateTableCommandHandler : IRequestHandler<UpdateTableCommand, Result<TableResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateTableCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<TableResponse>> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<TableResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var table = await _db.DiningTables.SingleOrDefaultAsync(
            t => t.Id == request.Id && t.TenantId == tenantId && t.DeletedAt == null, cancellationToken);
        if (table is null)
        {
            return Result<TableResponse>.Failure("Mesa não encontrada.", ApiErrorCodes.TableNotFound);
        }

        var area = await _db.Areas.SingleOrDefaultAsync(
            a => a.Id == request.AreaId && a.TenantId == tenantId && a.DeletedAt == null, cancellationToken);
        if (area is null)
        {
            return Result<TableResponse>.Failure("Ambiente não encontrado.", ApiErrorCodes.AreaNotFound);
        }

        var label = request.Label.Trim();
        var labelTaken = await _db.DiningTables.AnyAsync(
            t => t.Id != table.Id && t.StoreId == table.StoreId && t.Label == label && t.DeletedAt == null, cancellationToken);
        if (labelTaken)
        {
            return Result<TableResponse>.Failure(
                $"Já existe uma mesa com o rótulo \"{label}\" nesta loja.", ApiErrorCodes.TableLabelAlreadyExists);
        }

        var now = DateTimeOffset.UtcNow;
        var before = JsonSerializer.Serialize(new { table.Label, table.Seats, table.AreaId, table.SortOrder });

        table.Rename(label, request.Seats, area.Id, request.SortOrder);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "TABLE_UPDATED",
            entity: "dining_table",
            occurredAt: now,
            storeId: table.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: table.Id,
            before: before,
            after: JsonSerializer.Serialize(new { table.Label, table.Seats, table.AreaId, table.SortOrder })));

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "dining_table",
            aggregateId: table.Id,
            payload: JsonSerializer.Serialize(new { areaId = table.AreaId, tableId = table.Id }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: table.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        return Result<TableResponse>.Success(new TableResponse(
            table.Id, table.AreaId, area.Name, table.Label, table.Seats, table.Status.ToString().ToUpperInvariant(), table.IsActive, table.SortOrder));
    }
}
