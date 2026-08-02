namespace Nexora.Infrastructure.Devices;

/// <summary>
/// Pepper de HMAC para o hash de código de pareamento e de segredo de dispositivo — porta de
/// <c>DEVICE_HASH_PEPPER</c> (<c>apps/api-edge/src/modules/devices/devices.module.ts</c>).
/// Configurado via appsettings/Secret Manager/variável de ambiente — nunca versionado.
/// </summary>
public sealed class DeviceSecurityOptions
{
    public const string SectionName = "Devices:Security";

    public string Pepper { get; set; } = string.Empty;
}
