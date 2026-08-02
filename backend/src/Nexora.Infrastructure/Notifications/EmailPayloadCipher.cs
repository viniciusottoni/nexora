using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// Envelope cifrado AES-256-GCM para as variáveis de um e-mail transacional, no formato
/// <c>enc:v1:&lt;iv&gt;:&lt;tag&gt;:&lt;ciphertext&gt;</c> (tudo em base64url) — mesmo esquema de
/// <c>InvitationPayloadCipher</c> do NestJS original, para que um worker de entrega compatível
/// consiga decifrar sem mudança de formato.
/// </summary>
public static class EmailPayloadCipher
{
    public static string Encrypt(IReadOnlyDictionary<string, string> payload, string secret)
    {
        var key = DeriveKey(secret);
        var iv = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(iv, plaintext, ciphertext, tag);

        return string.Join(
            ':',
            "enc",
            "v1",
            Base64Url(iv),
            Base64Url(tag),
            Base64Url(ciphertext));
    }

    public static IReadOnlyDictionary<string, string> Decrypt(string envelope, string secret)
    {
        var parts = envelope.Split(':');
        if (parts.Length != 5 || parts[0] != "enc" || parts[1] != "v1")
            throw new InvalidOperationException("Envelope de e-mail inválido.");

        var key = DeriveKey(secret);
        var iv = FromBase64Url(parts[2]);
        var tag = FromBase64Url(parts[3]);
        var ciphertext = FromBase64Url(parts[4]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(plaintext)
               ?? new Dictionary<string, string>();
    }

    private static byte[] DeriveKey(string secret) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(secret));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
