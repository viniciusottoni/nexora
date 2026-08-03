using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Areas.Commands.SetAreaActive;

internal sealed class SetAreaActiveCommandHandler : IRequestHandler<SetAreaActiveCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public SetAreaActiveCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(SetAreaActiveCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var area = await _db.Areas.SingleOrDefaultAsync(
            a => a.Id == request.Id && a.TenantId == tenantId && a.DeletedAt == null, cancellationToken);
        if (area is null)
        {
            return Result.Failure("Ambiente não encontrado.", ApiErrorCodes.AreaNotFound);
        }

        if (request.Active) area.Activate(); else area.Deactivate();

        _db.AuditLogs.Add(Domain.Platform.AuditLog.Create(
            area.TenantId,
            action: request.Active ? "AREA_ACTIVATED" : "AREA_DEACTIVATED",
            entity: "area",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: area.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: area.Id));

        return Result.Success();
    }
}
