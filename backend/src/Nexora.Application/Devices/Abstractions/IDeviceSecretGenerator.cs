namespace Nexora.Application.Devices.Abstractions;

/// <summary>
/// Gera o segredo do dispositivo entregue uma única vez no momento do pareamento (nunca
/// reexibido nem persistido em claro — só o hash via <c>ISecretDigester</c>) — porta de
/// <c>secret: () => randomBytes(32).toString('base64url')</c> em
/// <c>apps/api-edge/src/modules/devices/devices.module.ts</c>.
/// </summary>
public interface IDeviceSecretGenerator
{
    string Generate();
}
