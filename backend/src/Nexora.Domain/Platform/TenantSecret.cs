using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Credencial criptografada em repouso (ADR-031), ex.: token de gateway de pagamento.
/// Nunca retorna pela API, nem para o OWNER; nunca trafega para o edge. Chave primária
/// composta (tenant_id, key).
/// </summary>
public sealed class TenantSecret
{
    private TenantSecret() { }

    public Guid TenantId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public byte[] Ciphertext { get; private set; } = Array.Empty<byte>();
    public int KeyVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TenantSecret Create(Guid tenantId, string key, byte[] ciphertext, int keyVersion)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("A chave do segredo do tenant é obrigatória.");

        if (ciphertext is null || ciphertext.Length == 0)
            throw new DomainException("O conteúdo cifrado do segredo do tenant é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new TenantSecret
        {
            TenantId = tenantId,
            Key = key,
            Ciphertext = ciphertext,
            KeyVersion = keyVersion,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Rotate(byte[] ciphertext, int keyVersion)
    {
        if (ciphertext is null || ciphertext.Length == 0)
            throw new DomainException("O conteúdo cifrado do segredo do tenant é obrigatório.");

        Ciphertext = ciphertext;
        KeyVersion = keyVersion;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
