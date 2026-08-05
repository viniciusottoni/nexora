using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Histórico de incidente de uma instalação edge (US-140 §3.1 "Histórico de incidentes por
/// instalação") — aberto quando o painel de saúde da plataforma detecta uma transição para
/// DEGRADED ou DOWN (ver <see cref="EdgeInstallation.RecordHeartbeat"/> e o worker que consome
/// <c>edge.offline_detected</c>/<c>sync.delayed</c>) e fechado quando a instalação volta a ficar
/// saudável. Regra RN-015: expõe apenas saúde técnica, nunca dado de negócio do tenant.
/// </summary>
public sealed class InstallationIncident
{
    private InstallationIncident() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid InstallationId { get; private set; }
    public InstallationIncidentType Type { get; private set; }
    public string? Cause { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsOpen => ResolvedAt is null;

    public static InstallationIncident Open(
        Guid tenantId, Guid installationId, InstallationIncidentType type, string? cause, DateTimeOffset startedAt)
    {
        var now = DateTimeOffset.UtcNow;

        return new InstallationIncident
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            InstallationId = installationId,
            Type = type,
            Cause = cause,
            StartedAt = startedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Resolve(DateTimeOffset resolvedAt)
    {
        if (ResolvedAt is not null)
            return;

        ResolvedAt = resolvedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Tipo de incidente registrado — espelha as classificações do painel de saúde (US-140 §10).</summary>
public enum InstallationIncidentType
{
    Offline = 0,
    Degraded = 1
}
