using System.Globalization;
using System.Security.Cryptography;
using Nexora.Application.Devices.Abstractions;

namespace Nexora.Infrastructure.Devices;

/// <summary>
/// Código de pareamento de 6 dígitos, criptograficamente aleatório — porta de
/// <c>randomInt(0, 1_000_000)</c> (Node <c>crypto</c>, também um CSPRNG) em
/// <c>apps/api-edge/src/modules/devices/devices.module.ts</c>.
/// </summary>
public sealed class PairingCodeGenerator : IPairingCodeGenerator
{
    public string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
}
