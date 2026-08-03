using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;

namespace Nexora.Application.Areas.Commands.CreateArea;

internal sealed class CreateAreaCommandHandler : IRequestHandler<CreateAreaCommand, Result<AreaResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public CreateAreaCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AreaResponse>> Handle(CreateAreaCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.StoreId is null)
        {
            return Result<AreaResponse>.Failure(
                "Não foi possível identificar o estabelecimento e a loja vinculados ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var storeId = _tenantContext.StoreId.Value;
        var now = DateTimeOffset.UtcNow;

        var area = Area.Create(tenantId, storeId, request.Name.Trim(), request.Position);
        _db.Areas.Add(area);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "AREA_CREATED",
            entity: "area",
            occurredAt: now,
            storeId: storeId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: area.Id,
            after: JsonSerializer.Serialize(new { area.Name, area.SortOrder })));

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "area",
            aggregateId: area.Id,
            payload: JsonSerializer.Serialize(new { areaId = area.Id, tableId = (Guid?)null }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: storeId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior (ADR-006: estado + evento na mesma transação).

        return Result<AreaResponse>.Success(new AreaResponse(area.Id, area.Name, area.SortOrder, area.IsActive, TableCount: 0));
    }
}
