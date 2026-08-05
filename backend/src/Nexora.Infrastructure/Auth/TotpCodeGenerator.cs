using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Nexora.Infrastructure.Auth;

/// <summary>
/// Gera códigos TOTP de 6 dígitos para um segredo base32.
/// </summary>
public static class TotpCodeGenerator
{
    private const int DigitCount = 6;
    private const int StepSeconds = 30;

    public static string Current(string secret, DateTimeOffset? utcNow = null)
    {
        var now = utcNow ?? DateTimeOffset.UtcNow;
        var key = Base32Decode(secret);
        var step = now.ToUnixTimeSeconds() / StepSeconds;
        return ComputeCode(key, step);
    }

    public static int SecondsRemaining(DateTimeOffset? utcNow = null)
    {
        var now = utcNow ?? DateTimeOffset.UtcNow;
        var remaining = StepSeconds - (int)(now.ToUnixTimeSeconds() % StepSeconds);
        return remaining == 0 ? StepSeconds : remaining;
    }

    private static string ComputeCode(byte[] key, long step)
    {
        var counter = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) |
                     ((hash[offset + 1] & 0xFF) << 16) |
                     ((hash[offset + 2] & 0xFF) << 8) |
                     (hash[offset + 3] & 0xFF);
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] Base32Decode(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var character in value.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(character);
            if (index < 0)
            {
                continue;
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft < 8)
            {
                continue;
            }

            bitsLeft -= 8;
            result.Add((byte)((buffer >> bitsLeft) & 0xFF));
        }

        return result.ToArray();
    }
}
