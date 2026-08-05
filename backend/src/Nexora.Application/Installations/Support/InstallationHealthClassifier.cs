namespace Nexora.Application.Installations.Support;

/// <summary>Classificação de saúde de uma instalação edge (US-140 §4, §10 — "saudável, degradada, fora do ar").</summary>
public enum InstallationHealthStatus
{
    Ok = 0,
    Degraded = 1,
    Down = 2
}

/// <summary>
/// Classificador puro de saúde de instalação (US-140 §9 "o painel é de nuvem... a detecção é da
/// nuvem, não do edge") — decide OK/DEGRADED/DOWN só a partir da defasagem de <c>LastSeenAt</c>
/// observada pela nuvem, nunca dos eventos <c>edge.offline_detected</c>/<c>edge.reconnected</c>
/// (esses são a visão do PRÓPRIO edge sobre sua conectividade de saída — podem nunca chegar se a
/// instalação está realmente fora do ar). Sem estado, sem I/O — testável isoladamente.
/// </summary>
/// <remarks>
/// Limiares são configuração global do produto (ADR-013: proibido condicional por tenant) — os
/// mesmos 5/15 minutos já usados como referência por <c>AlertThresholds.SyncDelayMinutes</c>
/// (docs/domain/01-Plataforma-e-Identidade.md, thresholds JSON) para DEGRADED, com DOWN em um
/// múltiplo (3x) desse valor: defasagem "ainda sincronizando, mas atrasado" é degradação;
/// ausência de contato por tempo bem maior (ou nenhum contato registrado) é queda.
/// </remarks>
public static class InstallationHealthClassifier
{
    /// <summary>Acima desta defasagem sem contato, a instalação é DEGRADED (não mais OK).</summary>
    public static readonly TimeSpan DegradedThreshold = TimeSpan.FromMinutes(5);

    /// <summary>Acima desta defasagem sem contato (ou <c>lastSeenAt</c> nulo), a instalação é DOWN.</summary>
    public static readonly TimeSpan DownThreshold = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Classifica a saúde da instalação em <paramref name="now"/> a partir do último contato
    /// conhecido. <paramref name="lastSeenAt"/> nulo (nunca sincronizou) é sempre DOWN — ADR-018
    /// exige que <paramref name="now"/> chegue em UTC (a leitura de <c>DateTimeOffset.UtcNow</c>
    /// é responsabilidade do chamador, nunca deste método, para manter o classificador puro/testável).
    /// </summary>
    public static InstallationHealthStatus Classify(DateTimeOffset now, DateTimeOffset? lastSeenAt)
    {
        if (lastSeenAt is null)
            return InstallationHealthStatus.Down;

        var staleness = now - lastSeenAt.Value;

        if (staleness >= DownThreshold)
            return InstallationHealthStatus.Down;

        if (staleness >= DegradedThreshold)
            return InstallationHealthStatus.Degraded;

        return InstallationHealthStatus.Ok;
    }

    /// <summary>Rótulo estável do contrato de API (US-140 §7 — "OK"/"DEGRADED"/"DOWN").</summary>
    public static string ToWireLabel(this InstallationHealthStatus status) => status switch
    {
        InstallationHealthStatus.Ok => "OK",
        InstallationHealthStatus.Degraded => "DEGRADED",
        InstallationHealthStatus.Down => "DOWN",
        _ => "OK"
    };
}
