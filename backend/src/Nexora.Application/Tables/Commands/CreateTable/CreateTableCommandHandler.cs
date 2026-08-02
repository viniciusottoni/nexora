using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Operation.Abstractions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.CreateTable;

internal sealed class CreateTableCommandHandler : IRequestHandler<CreateTableCommand, Result<TableResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IQrTokenGenerator _qrTokenGenerator;

    public CreateTableCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IQrTokenGenerator qrTokenGenerator)
    {
        _db = db;
        _tenantContext = tenantContext;
        _qrTokenGenerator = qrTokenGenerator;
    }

    public async Task<Result<TableResponse>> Handle(CreateTableCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.StoreId is null)
        {
            return Result<TableResponse>.Failure(
                "Não foi possível identificar o estabelecimento e a loja vinculados ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var storeId = _tenantContext.StoreId.Value;
        var label = request.Label.Trim();

        var area = await _db.Areas.SingleOrDefaultAsync(
            a => a.Id == request.AreaId && a.TenantId == tenantId && a.DeletedAt == null, cancellationToken);
        if (area is null)
        {
            return Result<TableResponse>.Failure("Ambiente não encontrado.", ApiErrorCodes.AreaNotFound);
        }

        var labelTaken = await _db.DiningTables.AnyAsync(
            t => t.StoreId == storeId && t.Label == label && t.DeletedAt == null, cancellationToken);
        if (labelTaken)
        {
            return Result<TableResponse>.Failure(
                $"Já existe uma mesa com o rótulo \"{label}\" nesta loja.", ApiErrorCodes.TableLabelAlreadyExists);
        }

        var table = DiningTable.Create(tenantId, storeId, area.Id, label, _qrTokenGenerator.Generate(), request.Seats);
        _db.DiningTables.Add(table);

        RecordCreation(table, area.Name);

        return Result<TableResponse>.Success(new TableResponse(
            table.Id, table.AreaId, area.Name, table.Label, table.Seats, table.Status.ToString().ToUpperInvariant(), table.IsActive, table.SortOrder));
    }

    private void RecordCreation(DiningTable table, string areaName)
    {
        var now = DateTimeOffset.UtcNow;

        _db.AuditLogs.Add(AuditLog.Create(
            table.TenantId,
            action: "TABLE_CREATED",
            entity: "dining_table",
            occurredAt: now,
            storeId: table.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: table.Id,
            after: JsonSerializer.Serialize(new { table.Label, table.Seats, areaId = table.AreaId, areaName })));

        _db.DomainEvents.Add(DomainEvent.Create(
            table.TenantId,
            type: "tenant.config_updated",
            aggregateType: "dining_table",
            aggregateId: table.Id,
            payload: JsonSerializer.Serialize(new { areaId = table.AreaId, tableId = table.Id }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: table.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));
    }
}
