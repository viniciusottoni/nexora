using System.Security.Cryptography;
using System.Text;
using Nexora.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Devices;

/// <summary>
/// HMAC-SHA256 com pepper — porta de <c>hashDeviceCredential</c>
/// (<c>apps/api-edge/src/modules/devices/devices.module.ts</c>). Usado tanto para o hash do
/// código de pareamento (<c>codeHash</c>) quanto para o segredo do dispositivo
/// (<c>secretHash</c>), exatamente como no TypeScript original — a mesma função <c>hash</c>
/// injetada era usada nos dois casos.
///
/// Implementação hoje vive em <c>Infrastructure/Devices</c> por ser o primeiro consumidor de
/// <see cref="ISecretDigester"/>; se o módulo de Auth (refresh token) também precisar dela,
/// mover para <c>Infrastructure/Security</c> com uma pepper própria por finalidade.
///
/// ATENÇÃO — risco de colisão de DI: <c>AcceptOwnerInvitationCommandHandler</c>
/// (Nexora.Application.Tenants) já injeta <see cref="ISecretDigester"/> para o segredo do
/// convite de dono, um propósito diferente do hash de código de pareamento/segredo de
/// dispositivo daqui. A interface, do jeito que está (sem escopo por finalidade), só suporta
/// UM registro de DI — o último `AddSingleton&lt;ISecretDigester, ...&gt;()` a rodar vence e o
/// outro módulo passa a usar a pepper errada silenciosamente. Antes de montar a composição
/// raiz (Program.cs) de qualquer Api, resolver isso com serviços keyed do .NET 8+
/// (`AddKeyedSingleton`) ou interfaces separadas por finalidade (`IDeviceSecretDigester` vs.
/// `IInviteSecretDigester`) — não registrar as duas implementações sob a mesma interface sem
/// chave.
/// </summary>
public sealed class DeviceSecretDigester : ISecretDigester
{
    private readonly byte[] _pepper;

    public DeviceSecretDigester(IOptions<DeviceSecurityOptions> options)
    {
        var pepper = options.Value.Pepper;
        if (string.IsNullOrWhiteSpace(pepper))
        {
            throw new InvalidOperationException(
                "Devices:Security:Pepper não configurado (equivalente a DEVICE_HASH_PEPPER no TypeScript original).");
        }

        _pepper = Encoding.UTF8.GetBytes(pepper);
    }

    public string Digest(string value)
    {
        using var hmac = new HMACSHA256(_pepper);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
