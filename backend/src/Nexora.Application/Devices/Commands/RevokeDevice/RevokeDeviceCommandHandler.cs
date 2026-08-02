using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices; // DeviceSnapshot
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Devices.Commands.RevokeDevice;

internal sealed class RevokeDeviceCommandHandler : IRequestHandler<RevokeDeviceCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<RevokeDeviceCommandHandler> _logger;

    public RevokeDeviceCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        ILogger<RevokeDeviceCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeDeviceCommand request, CancellationToken cancellationToken)
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

        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && d.TenantId == tenantId, cancellationToken);

        if (device is null)
        {
            return Result.Failure("Dispositivo não encontrado.", ApiErrorCodes.DeviceNotFound);
        }

        var before = DeviceSnapshot.ToJson(device);
        var now = DateTimeOffset.UtcNow;

        device.Deactivate();

        // Encerra toda sessão ativa do dispositivo revogado — porta de revokeSessions
        // (device-registry.ts), na mesma transação da revogação.
        var activeSessions = await _db.AuthSessions
            .Where(s => s.DeviceId == device.Id && s.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.Revoke();
        }

        var after = DeviceSnapshot.ToJson(device);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "DEVICE_REVOKED",
            entity: "device",
            occurredAt: now,
            storeId: device.StoreId,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId ?? device.Id,
            entityId: device.Id,
            before: before,
            after: after));

        _logger.LogInformation(
            "Dispositivo revogado. TenantId={TenantId}, DeviceId={DeviceId}, SessõesEncerradas={SessionCount}",
            tenantId, device.Id, activeSessions.Count);

        return Result.Success();
    }
}
