using System.Text.Json;
using Nexora.Domain.Platform;

namespace Nexora.Application.Devices;

/// <summary>
/// Porta de <c>snapshot(device)</c> em <c>device-registry.ts</c> — serializa o estado do
/// dispositivo para os campos <c>before</c>/<c>after</c> (JSONB) do <see cref="AuditLog"/>.
/// </summary>
internal static class DeviceSnapshot
{
    public static string ToJson(Device device) => JsonSerializer.Serialize(new
    {
        id = device.Id,
        label = device.Label,
        kind = DeviceKindMapper.ToKindCode(device.Type),
        fingerprint = device.Fingerprint,
        active = device.IsActive,
        lastSeenAt = device.LastSeenAt,
    });
}
