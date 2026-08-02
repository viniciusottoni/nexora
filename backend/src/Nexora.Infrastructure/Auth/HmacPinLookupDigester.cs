using System.Security.Cryptography;
using System.Text;
using Nexora.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Auth;

/// <summary>
/// Digest determinístico e não reversível de PIN para lookup (nunca para verificação de PIN,
/// que continua sendo feita via <see cref="ICredentialHasher"/>) — porta de pin-lookup-digester.ts.
/// HMAC-SHA256 com pimenta de configuração, saída hex minúscula.
/// </summary>
public sealed class HmacPinLookupDigester : IPinLookupDigester
{
    private readonly byte[] _pepper;

    public HmacPinLookupDigester(IOptions<AuthSecretsOptions> options)
    {
        var pepper = options.Value.PinLookupPepper;
        if (string.IsNullOrEmpty(pepper) || Encoding.UTF8.GetByteCount(pepper) < 32)
        {
            throw new InvalidOperationException("Auth:Secrets:PinLookupPepper deve ter ao menos 32 caracteres.");
        }

        _pepper = Encoding.UTF8.GetBytes(pepper);
    }

    public string Digest(string pin)
    {
        var hash = HMACSHA256.HashData(_pepper, Encoding.UTF8.GetBytes(pin));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
