using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.SetTableActive;

internal sealed class SetTableActiveCommandHandler : IRequestHandler<SetTableActiveCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public SetTableActiveCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(SetTableActiveCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var table = await _db.DiningTables.SingleOrDefaultAsync(
            t => t.Id == request.TableId && t.TenantId == tenantId && t.DeletedAt == null, cancellationToken);
        if (table is null)
        {
            return Result.Failure("Mesa não encontrada.", ApiErrorCodes.TableNotFound);
        }

        if (request.Active) table.Activate(); else table.Deactivate();

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: request.Active ? "TABLE_ACTIVATED" : "TABLE_DEACTIVATED",
            entity: "dining_table",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: table.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: table.Id));

        return Result.Success();
    }
}
