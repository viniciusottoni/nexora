using System.Globalization;
using System.Security.Cryptography;
using Nexora.Application.Abstractions.Security;

namespace Nexora.Infrastructure.Auth;

/// <summary>
/// Verificação de código TOTP (RFC 6238) — porta de TotpVerifier (biblioteca `otplib` no NestJS,
/// apps/api-cloud/src/modules/auth/crypto-adapters.ts). Implementado só com a BCL (HMAC-SHA1,
/// 6 dígitos, passo de 30 s) porque nenhum pacote de TOTP está referenciado na solution — decisão
/// registrada no relatório de porte. Janela de tolerância de ±1 passo, igual ao padrão do
/// `authenticator.check()` do otplib.
/// </summary>
public sealed class TotpOtpVerifier : IOtpVerifier
{
    private const int DigitCount = 6;
    private const int StepSeconds = 30;
    private const int ToleranceSteps = 1;

    public bool Verify(string secret, string otp)
    {
        if (string.IsNullOrWhiteSpace(otp) || otp.Length != DigitCount || !otp.All(char.IsDigit))
        {
            return false;
        }

        var key = Base32Decode(secret);
        if (key.Length == 0)
        {
            return false;
        }

        var currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;

        for (var offset = -ToleranceSteps; offset <= ToleranceSteps; offset++)
        {
            if (ComputeCode(key, currentStep + offset) == otp)
            {
                return true;
            }
        }

        return false;
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
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var code = binary % (int)Math.Pow(10, DigitCount);
        return code.ToString(new string('0', DigitCount), CultureInfo.InvariantCulture);
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var cleaned = base32.Trim().TrimEnd('=').ToUpperInvariant();

        var bits = new List<byte>(cleaned.Length * 5 / 8);
        int buffer = 0, bitsLeft = 0;
        foreach (var c in cleaned)
        {
            var index = alphabet.IndexOf(c);
            if (index < 0)
            {
                continue;
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                bits.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return bits.ToArray();
    }
}
