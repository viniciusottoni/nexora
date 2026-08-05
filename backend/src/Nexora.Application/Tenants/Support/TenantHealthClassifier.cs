using Nexora.Application.Installations.Support;

namespace Nexora.Application.Tenants.Support;

/// <summary>US-151 §2 "saúde da instalação" agregada por tenant (contrato <c>health</c>, uma das colunas do diretório).</summary>
public enum TenantHealthStatus
{
    /// <summary>Tenant sem nenhuma instalação com <c>InstalledAt != null</c> — nunca reportar DOWN por engano (instrução explícita da US).</summary>
    Unknown,
    Ok,
    Degraded,
    Down
}

/// <summary>
/// Agrega o pior <see cref="InstallationHealthStatus"/> entre as instalações INSTALADAS de um
/// tenant (mesmo filtro <c>InstalledAt != null</c> de <c>GetPlatformSummaryQueryHandler</c>) num
/// único <see cref="TenantHealthStatus"/> por linha do diretório. Sem estado, sem I/O — a leitura
/// de <c>edge_installation.last_seen_at</c> é responsabilidade do chamador (mesmo contrato de
/// <see cref="InstallationHealthClassifier.Classify"/>).
/// </summary>
public static class TenantHealthClassifier
{
    public static TenantHealthStatus Classify(DateTimeOffset now, IReadOnlyCollection<DateTimeOffset?> installedLastSeenAtValues)
    {
        if (installedLastSeenAtValues.Count == 0)
            return TenantHealthStatus.Unknown;

        var worst = InstallationHealthStatus.Ok;

        foreach (var lastSeenAt in installedLastSeenAtValues)
        {
            var status = InstallationHealthClassifier.Classify(now, lastSeenAt);
            if (status > worst)
                worst = status;
        }

        return worst switch
        {
            InstallationHealthStatus.Down => TenantHealthStatus.Down,
            InstallationHealthStatus.Degraded => TenantHealthStatus.Degraded,
            _ => TenantHealthStatus.Ok
        };
    }

    /// <summary>Rótulo estável do contrato de API (US-151 §7 — "OK"/"DEGRADED"/"DOWN"/"UNKNOWN").</summary>
    public static string ToWireLabel(this TenantHealthStatus status) => status switch
    {
        TenantHealthStatus.Ok => "OK",
        TenantHealthStatus.Degraded => "DEGRADED",
        TenantHealthStatus.Down => "DOWN",
        _ => "UNKNOWN"
    };
}
