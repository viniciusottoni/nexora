using System.Security.Cryptography;
using Nexora.Application.Devices.Abstractions;

namespace Nexora.Infrastructure.Devices;

/// <summary>
/// Segredo de dispositivo entregue uma única vez no pareamento — porta de
/// <c>randomBytes(32).toString('base64url')</c> em
/// <c>apps/api-edge/src/modules/devices/devices.module.ts</c>.
/// </summary>
public sealed class DeviceSecretGenerator : IDeviceSecretGenerator
{
    public string Generate() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
