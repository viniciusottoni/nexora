using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Instalação do servidor edge (mini-PC) de uma loja — autoridade operacional local
/// de pedido, mesa, comanda, KDS e caixa. Uma instalação por loja (uq_edge_store).
/// </summary>
public sealed class EdgeInstallation
{
    private EdgeInstallation() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string? PublicKey { get; private set; }
    public string? Version { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public long LastSyncedSeq { get; private set; }
    public int? ClockOffsetMs { get; private set; }

    // TODO: value object tipado quando o formato de health for definido — hoje é JSONB livre
    public string Health { get; private set; } = "{}";

    public string? InstallTokenHash { get; private set; }
    public DateTimeOffset? TokenExpiresAt { get; private set; }
    public DateTimeOffset? TokenConsumedAt { get; private set; }
    public DateTimeOffset? InstalledAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Store Store { get; private set; } = null!;

    /// <summary>Instalação já concluiu o pareamento (chave pública recebida e aceita).</summary>
    public bool IsInstalled => InstalledAt is not null;

    /// <summary>Token de instalação já foi reservado/consumido (ver <see cref="ReserveInstallToken"/>).</summary>
    public bool IsTokenConsumed => TokenConsumedAt is not null;

    public static EdgeInstallation Create(Guid tenantId, Guid storeId, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("O rótulo da instalação edge é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new EdgeInstallation
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Label = label,
            LastSyncedSeq = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Reconstrução direta do lado edge durante a importação do bootstrap (ADR-019 fluxo de
    /// primeira subida) — o container local já recebeu seu próprio id (INSTALLATION_ID) e a
    /// chave pública gerada localmente, sem passar pelo protocolo de token da nuvem.
    /// Espelha o branch "create" de <c>persistBootstrap</c> (import-bootstrap.ts).
    /// </summary>
    public static EdgeInstallation CreateInstalled(
        Guid id,
        Guid tenantId,
        Guid storeId,
        string label,
        string publicKey,
        string version)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new DomainException("A chave pública da instalação edge é obrigatória.");

        var now = DateTimeOffset.UtcNow;

        return new EdgeInstallation
        {
            Id = id,
            TenantId = tenantId,
            StoreId = storeId,
            Label = string.IsNullOrWhiteSpace(label) ? "Edge local" : label,
            PublicKey = publicKey,
            Version = version,
            LastSyncedSeq = 0,
            InstalledAt = now,
            TokenConsumedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void IssueInstallToken(string installTokenHash, DateTimeOffset tokenExpiresAt)
    {
        InstallTokenHash = installTokenHash;
        TokenExpiresAt = tokenExpiresAt;
        TokenConsumedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsTokenExpired(DateTimeOffset now) => TokenExpiresAt is null || now >= TokenExpiresAt;

    /// <summary>
    /// Reserva o token de uso único sem concluir o pareamento — usado pelo passo de
    /// "consumo do token" que precede o registro físico do edge (a chave pública só chega
    /// depois, quando o dispositivo efetivamente sobe — ver <see cref="CompleteRegistration"/>).
    /// Idempotência/replay do token em si é responsabilidade do handler (ADR-020 aplica-se à
    /// escrita HTTP; aqui é reforçado pela checagem de <see cref="IsTokenConsumed"/>).
    /// </summary>
    public void ReserveInstallToken()
    {
        TokenConsumedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkInstalled(string publicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new DomainException("A chave pública da instalação edge é obrigatória.");

        PublicKey = publicKey;
        InstalledAt = DateTimeOffset.UtcNow;
        TokenConsumedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Conclui o registro iniciado pelo protocolo de token da nuvem (POST .../installations/register
    /// — RegisterInstallationCommand), gravando também rótulo/versão informados pelo dispositivo.
    /// Idempotente: reenviar a mesma chave pública não é erro (ver RegisterInstallationCommandHandler).
    /// </summary>
    public void CompleteRegistration(string publicKey, string? version, string? label)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new DomainException("A chave pública da instalação edge é obrigatória.");

        PublicKey = publicKey;

        if (!string.IsNullOrWhiteSpace(version))
            Version = version;

        if (!string.IsNullOrWhiteSpace(label))
            Label = label;

        InstalledAt = DateTimeOffset.UtcNow;
        TokenConsumedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordHeartbeat(long syncedSeq, int? clockOffsetMs, string? healthJson = null)
    {
        LastSeenAt = DateTimeOffset.UtcNow;
        LastSyncedSeq = syncedSeq;
        ClockOffsetMs = clockOffsetMs;

        if (healthJson is not null)
            Health = healthJson;

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
