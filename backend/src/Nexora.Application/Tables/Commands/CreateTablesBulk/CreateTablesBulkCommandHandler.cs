using System.Globalization;
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

namespace Nexora.Application.Tables.Commands.CreateTablesBulk;

/// <summary>
/// Cenário Gherkin "Criação em lote" da US-020. Todas as mesas do intervalo são adicionadas ao
/// mesmo <c>DbContext</c> sem <c>SaveChangesAsync</c> intermediário — o único commit acontece no
/// <c>TransactionBehavior</c>, depois que o handler inteiro retorna; se qualquer mesa do lote
/// violasse uma constraint (ex.: rótulo duplicado que escapou da checagem abaixo por uma corrida),
/// o banco recusaria a transação inteira, nenhuma mesa parcial ficaria gravada.
/// </summary>
internal sealed class CreateTablesBulkCommandHandler : IRequestHandler<CreateTablesBulkCommand, Result<TablesBulkResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IQrTokenGenerator _qrTokenGenerator;

    public CreateTablesBulkCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IQrTokenGenerator qrTokenGenerator)
    {
        _db = db;
        _tenantContext = tenantContext;
        _qrTokenGenerator = qrTokenGenerator;
    }

    public async Task<Result<TablesBulkResponse>> Handle(CreateTablesBulkCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.StoreId is null)
        {
            return Result<TablesBulkResponse>.Failure(
                "Não foi possível identificar o estabelecimento e a loja vinculados ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var storeId = _tenantContext.StoreId.Value;

        var area = await _db.Areas.SingleOrDefaultAsync(
            a => a.Id == request.AreaId && a.TenantId == tenantId && a.DeletedAt == null, cancellationToken);
        if (area is null)
        {
            return Result<TablesBulkResponse>.Failure("Ambiente não encontrado.", ApiErrorCodes.AreaNotFound);
        }

        var labels = Enumerable.Range(request.From, request.To - request.From + 1)
            .Select(n => n.ToString(CultureInfo.InvariantCulture))
            .ToList();

        var existingLabels = await _db.DiningTables
            .Where(t => t.StoreId == storeId && t.DeletedAt == null && labels.Contains(t.Label))
            .Select(t => t.Label)
            .ToListAsync(cancellationToken);

        if (existingLabels.Count > 0)
        {
            return Result<TablesBulkResponse>.Failure(
                $"Já existem mesas com os rótulos: {string.Join(", ", existingLabels.OrderBy(l => l))}.",
                ApiErrorCodes.TableLabelAlreadyExists);
        }

        var now = DateTimeOffset.UtcNow;
        var created = new List<DiningTable>(labels.Count);

        foreach (var label in labels)
        {
            var table = DiningTable.Create(tenantId, storeId, area.Id, label, _qrTokenGenerator.Generate(), request.Seats);
            _db.DiningTables.Add(table);
            created.Add(table);
        }

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "TABLE_BULK_CREATED",
            entity: "dining_table",
            occurredAt: now,
            storeId: storeId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: area.Id,
            after: JsonSerializer.Serialize(new { areaId = area.Id, from = request.From, to = request.To, count = created.Count })));

        foreach (var table in created)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "tenant.config_updated",
                aggregateType: "dining_table",
                aggregateId: table.Id,
                payload: JsonSerializer.Serialize(new { areaId = area.Id, tableId = table.Id }),
                origin: "CLOUD",
                occurredAt: now,
                storeId: storeId,
                actorId: _tenantContext.UserId,
                deviceId: _tenantContext.DeviceId));
        }

        var items = created
            .Select(t => new TableResponse(t.Id, t.AreaId, area.Name, t.Label, t.Seats, t.Status.ToString().ToUpperInvariant(), t.IsActive, t.SortOrder))
            .ToList();

        return Result<TablesBulkResponse>.Success(new TablesBulkResponse(items));
    }
}
