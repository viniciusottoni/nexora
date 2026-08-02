using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Nonce de uso único usado no protocolo de autenticação da instalação edge (anti-replay).
/// Chave primária composta (installation_id, nonce) — não tem Id próprio.
/// </summary>
public sealed class InstallationNonce
{
    private InstallationNonce() { }

    public Guid InstallationId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Nonce { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static InstallationNonce Create(Guid installationId, Guid tenantId, string nonce, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(nonce))
            throw new DomainException("O nonce da instalação é obrigatório.");

        return new InstallationNonce
        {
            InstallationId = installationId,
            TenantId = tenantId,
            Nonce = nonce,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
