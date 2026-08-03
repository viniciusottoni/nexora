using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices; // DeviceSnapshot
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Devices.Commands.DeleteDevice;

internal sealed class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public DeleteDeviceCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var device = await _db.Devices
            .FirstOrDefaultAsync(
                d => d.Id == request.DeviceId && d.TenantId == tenantId && d.DeletedAt == null,
                cancellationToken);

        if (device is null)
        {
            return Result.Failure("Dispositivo não encontrado.", ApiErrorCodes.DeviceNotFound);
        }

        if (device.IsActive)
        {
            return Result.Failure(
                "Revogue o acesso do dispositivo antes de excluí-lo da lista.",
                ApiErrorCodes.DeviceMustBeRevokedBeforeDelete);
        }

        var before = DeviceSnapshot.ToJson(device);

        device.SoftDelete();

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "DEVICE_DELETED",
            entity: "device",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: device.StoreId,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: device.Id,
            before: before));

        return Result.Success();
    }
}
