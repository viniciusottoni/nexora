using System.Security.Cryptography;
using System.Text;
using Nexora.Application.Installations.Abstractions;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Installations;

/// <summary>Porta de <c>deriveTenantPinPepper</c> — HMAC-SHA256(masterKey, "pin-lookup:{tenantId}").</summary>
public sealed class PinLookupPepperProvider : IPinLookupPepperProvider
{
    private readonly byte[] _masterKey;

    public PinLookupPepperProvider(IOptions<PinLookupMasterKeyOptions> options)
    {
        var masterKey = options.Value.Value;
        if (string.IsNullOrEmpty(masterKey) || Encoding.UTF8.GetByteCount(masterKey) < 32)
        {
            throw new InvalidOperationException(
                "Installations:PinLookupMasterKey deve ter ao menos 32 caracteres (equivalente a PIN_LOOKUP_MASTER_KEY no original).");
        }

        _masterKey = Encoding.UTF8.GetBytes(masterKey);
    }

    public string Derive(Guid tenantId)
    {
        var hash = HMACSHA256.HashData(_masterKey, Encoding.UTF8.GetBytes($"pin-lookup:{tenantId}"));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
