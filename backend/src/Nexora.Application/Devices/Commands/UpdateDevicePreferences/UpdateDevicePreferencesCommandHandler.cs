using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Devices.Support;
using Nexora.Contracts.Devices;
using Nexora.Shared.Errors;
using Nexora.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Devices.Commands.UpdateDevicePreferences;

/// <summary>
/// US-042/US-045/US-047 — grava a preferência "por dispositivo" (praça filtrada, som, modo pico do
/// KDS). Sem checagem de <c>[Authorize(Policy=...)]</c> na Controller (diferente de
/// <c>RenameDeviceCommand</c>): a maioria das chamadas é o PRÓPRIO terminal configurando a SI
/// MESMO (o pizzaiolo ajustando o volume do próprio KDS), então a regra de autorização mora aqui —
/// autoatendimento sempre permitido (<c>request.DeviceId == _tenantContext.DeviceId</c>), edição de
/// OUTRO dispositivo exige <c>device:manage</c> (mesma permissão de <c>DeviceManage</c>, US-005).
/// </summary>
internal sealed class UpdateDevicePreferencesCommandHandler : IRequestHandler<UpdateDevicePreferencesCommand, Result<DevicePreferencesResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateDevicePreferencesCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<DevicePreferencesResponse>> Handle(UpdateDevicePreferencesCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            return Result<DevicePreferencesResponse>.Failure("Contexto de loja não identificado.", ApiErrorCodes.TenantContextMissing);
        }

        var isSelfService = _tenantContext.DeviceId == request.DeviceId;
        if (!isSelfService && !PermissionAuthorization.HasPermission(_tenantContext.Permissions, "device:manage"))
        {
            return Result<DevicePreferencesResponse>.Failure(
                "Só o próprio dispositivo ou um gestor pode alterar estas preferências.",
                ApiErrorCodes.AuthPermissionDenied);
        }

        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && d.TenantId == tenantId, cancellationToken);

        if (device is null)
        {
            return Result<DevicePreferencesResponse>.Failure("Dispositivo não encontrado.", ApiErrorCodes.DeviceNotFound);
        }

        var merged = DevicePreferencesJsonMerger.Merge(device.Preferences, request.PreferencesPatchJson);
        device.UpdatePreferences(merged);

        using var mergedDocument = JsonDocument.Parse(merged);
        return Result<DevicePreferencesResponse>.Success(
            new DevicePreferencesResponse(device.Id, mergedDocument.RootElement.Clone()));
    }
}
