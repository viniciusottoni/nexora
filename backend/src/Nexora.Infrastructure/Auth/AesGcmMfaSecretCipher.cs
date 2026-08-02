using System.Security.Cryptography;
using System.Text;
using Nexora.Application.Abstractions.Security;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Auth;

/// <summary>
/// Cifra do segredo MFA em repouso — porta de MfaSecretCipher (apps/api-cloud/src/modules/auth/crypto-adapters.ts).
/// Envelope AES-256-GCM <c>enc:v1:&lt;iv&gt;:&lt;tag&gt;:&lt;ciphertext&gt;</c> (base64url), chave
/// derivada por SHA-256 de <c>Auth:Secrets:MfaEncryptionKey</c> (porta de MFA_ENCRYPTION_KEY).
/// </summary>
public sealed class AesGcmMfaSecretCipher : IMfaSecretCipher
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmMfaSecretCipher(IOptions<AuthSecretsOptions> options)
    {
        var secret = options.Value.MfaEncryptionKey;
        if (string.IsNullOrEmpty(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("Auth:Secrets:MfaEncryptionKey deve ter ao menos 32 caracteres.");
        }

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag);

        return $"enc:v1:{Base64Url(nonce)}:{Base64Url(tag)}:{Base64Url(ciphertext)}";
    }

    public string Decrypt(string envelope)
    {
        var parts = envelope.Split(':');
        if (parts.Length != 5 || parts[0] != "enc" || parts[1] != "v1")
        {
            throw new InvalidOperationException("Segredo MFA não está em envelope criptografado válido.");
        }

        var nonce = Base64UrlDecode(parts[2]);
        var tag = Base64UrlDecode(parts[3]);
        var ciphertext = Base64UrlDecode(parts[4]);
        var plainBytes = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - (padded.Length % 4)) % 4);
        return Convert.FromBase64String(padded);
    }
}
