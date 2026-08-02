using System.Security.Cryptography;
using System.Text;
using Nexora.Application.Abstractions.Security;
using Konscious.Security.Cryptography;

namespace Nexora.Infrastructure.Auth;

/// <summary>
/// Hash Argon2id de PIN/senha — porta de ArgonCredentialHasher (apps/api-edge e apps/api-cloud,
/// argon-credential-hasher.ts/crypto-adapters.ts), mesmos parâmetros de custo do pacote `argon2`
/// do Node: memoryCost 19456 KiB, timeCost 2, parallelism 1. Codificação PHC
/// (<c>$argon2id$v=19$m=...,t=...,p=...$&lt;salt&gt;$&lt;hash&gt;</c>), compatível com o hash
/// gerado pela lib `argon2` (inclusive o usado no bootstrap do PLATFORM_ADMIN via
/// PLATFORM_ADMIN_PASSWORD_HASH).
/// </summary>
public sealed class Argon2CredentialHasher : ICredentialHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemoryKib = 19_456;
    private const int Iterations = 2;
    private const int Parallelism = 1;

    public string Hash(string plainText)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(plainText, salt, MemoryKib, Iterations, Parallelism, HashSize);
        return Encode(salt, hash);
    }

    public bool Verify(string hash, string plainText)
    {
        if (!TryDecode(hash, out var salt, out var expectedHash, out var memoryKib, out var iterations, out var parallelism))
        {
            return false;
        }

        var actualHash = ComputeHash(plainText, salt, memoryKib, iterations, parallelism, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(string plainText, byte[] salt, int memoryKib, int iterations, int parallelism, int hashSize)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(plainText))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            MemorySize = memoryKib,
            Iterations = iterations,
        };
        return argon2.GetBytes(hashSize);
    }

    private static string Encode(byte[] salt, byte[] hash) =>
        $"$argon2id$v=19$m={MemoryKib},t={Iterations},p={Parallelism}${Base64NoPad(salt)}${Base64NoPad(hash)}";

    private static string Base64NoPad(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

    private static bool TryDecode(
        string encoded, out byte[] salt, out byte[] hash, out int memoryKib, out int iterations, out int parallelism)
    {
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();
        memoryKib = MemoryKib;
        iterations = Iterations;
        parallelism = Parallelism;

        // formato: $argon2id$v=19$m=...,t=...,p=...$<salt>$<hash>
        var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || parts[0] != "argon2id")
        {
            return false;
        }

        foreach (var costPart in parts[2].Split(','))
        {
            var pair = costPart.Split('=');
            if (pair.Length != 2 || !int.TryParse(pair[1], out var value))
            {
                continue;
            }

            switch (pair[0])
            {
                case "m": memoryKib = value; break;
                case "t": iterations = value; break;
                case "p": parallelism = value; break;
            }
        }

        try
        {
            salt = Convert.FromBase64String(PadBase64(parts[3]));
            hash = Convert.FromBase64String(PadBase64(parts[4]));
        }
        catch (FormatException)
        {
            return false;
        }

        return true;
    }

    private static string PadBase64(string value)
    {
        var padding = (4 - (value.Length % 4)) % 4;
        return value + new string('=', padding);
    }
}
