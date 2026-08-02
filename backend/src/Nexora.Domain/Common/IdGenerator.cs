namespace Nexora.Domain.Common;

public static class IdGenerator
{
    // UUIDv7 na origem (ADR-016). API nativa a partir do .NET 9 (Guid.CreateVersion7).
    // Fallback manual (RFC 9562 §5.7) para compilar também em .NET 8, usado neste sandbox de migração.
    public static Guid NewId()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        Span<byte> bytes = stackalloc byte[16];
        Random.Shared.NextBytes(bytes);
        long unixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)(unixMs >> 40);
        bytes[1] = (byte)(unixMs >> 32);
        bytes[2] = (byte)(unixMs >> 24);
        bytes[3] = (byte)(unixMs >> 16);
        bytes[4] = (byte)(unixMs >> 8);
        bytes[5] = (byte)unixMs;
        bytes[6] = (byte)(0x70 | (bytes[6] & 0x0F)); // versão 7
        bytes[8] = (byte)(0x80 | (bytes[8] & 0x3F)); // variante RFC 4122
        return new Guid(bytes);
#endif
    }
}
