using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices; // DeviceSnapshot
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Devices.Commands.RenameDevice;

internal sealed class RenameDeviceCommandHandler : IRequestHandler<RenameDeviceCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<RenameDeviceCommandHandler> _logger;

    public RenameDeviceCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        ILogger<RenameDeviceCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result> Handle(RenameDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (_tenantContext.UserId is null)
        {
            return Result.Failure(
                "Esta ação exige um gestor autenticado.",
                ApiErrorCodes.DeviceActorRequired);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;

        // Tracked (não AsNoTracking): o handler muda o agregado e o TransactionBehavior salva.
        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && d.TenantId == tenantId, cancellationToken);

        if (device is null)
        {
            // 404 mesmo se o dispositivo existir em outro tenant — nunca 403 (ADR-021).
            return Result.Failure("Dispositivo não encontrado.", ApiErrorCodes.DeviceNotFound);
        }

        var before = DeviceSnapshot.ToJson(device);
        device.Rename(request.Label);
        var after = DeviceSnapshot.ToJson(device);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "DEVICE_RENAMED",
            entity: "device",
            occurredAt: DateTimeOffset.UtcNow,
            storeId: device.StoreId,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: device.Id,
            before: before,
            after: after));

        _logger.LogInformation(
            "Dispositivo renomeado. TenantId={TenantId}, DeviceId={DeviceId}", tenantId, device.Id);

        return Result.Success();
    }
}
