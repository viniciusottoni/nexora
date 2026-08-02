namespace Nexora.Infrastructure.Installation;

/// <summary>
/// Identidade/local dos segredos do edge — porta de <c>INSTALLATION_ID</c>,
/// <c>INSTALLATION_PRIVATE_KEY</c>, <c>SYNC_ENDPOINT</c> e <c>SYNC_HEALTH_INTERVAL_MS</c>
/// (<c>installation-request-auth.ts</c>/<c>sync-worker.ts</c>).
/// </summary>
public sealed class EdgeInstallationIdentityOptions
{
    public const string SectionName = "Edge:Installation";

    public Guid InstallationId { get; set; }

    /// <summary>Caminho do arquivo PEM com a chave privada Ed25519 (PKCS8) usada para assinar requisições à nuvem.</summary>
    public string PrivateKeyPath { get; set; } = "/run/secrets/edge/edge-private.pem";

    /// <summary>URL base da nuvem (ex.: <c>https://cloud.donabetinha.com.br</c>), sem <c>/v1/sync</c> ao final.</summary>
    public string SyncEndpoint { get; set; } = string.Empty;

    public int SyncHealthIntervalMs { get; set; } = 30_000;
}
