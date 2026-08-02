using System.Security.Cryptography;
using System.Text;
using Nexora.Application.Installations.Abstractions;

namespace Nexora.Infrastructure.Installations;

/// <summary>
/// SHA-256 puro, sem pepper — mesma operação de
/// <c>createHash('sha256').update(rawToken, 'utf8').digest('hex')</c> no original. O token de
/// instalação já é aleatório de alta entropia (gerado por <c>ITokenIssuer</c>/quem emite o
/// convite de provisionamento), então, diferente de PIN/senha, não precisa de pepper.
/// </summary>
public sealed class InstallationTokenDigester : IInstallationTokenDigester
{
    public string Digest(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
