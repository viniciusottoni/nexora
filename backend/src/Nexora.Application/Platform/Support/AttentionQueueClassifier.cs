using System.Globalization;

namespace Nexora.Application.Platform.Support;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — classificador PURO (sem I/O, sem
/// <c>DateTimeOffset.UtcNow</c> interno — ADR-018 exige que "agora" chegue de fora, mesmo contrato de
/// <c>InstallationHealthClassifier.Classify</c>/<c>TenantHealthClassifier.Classify</c>) de severidade
/// e motivo substantivo de cada tipo de pendência da fila de atenção. Limiares são
/// [HIPÓTESE] desta tarefa — a matriz de severidade/SLA é PENDÊNCIA aberta na especificação da
/// própria US-157 (nenhum documento anterior a define); a heurística escolhida aqui é conservadora e
/// reaproveita, onde fazia sentido, os limiares já aceitos por <c>InstallationHealthClassifier</c>
/// (DEGRADED/DOWN em 5/15 min) como ponto de partida, escalando para CRITICAL só quando a condição
/// persiste por bem mais tempo — nunca no instante em que é detectada.
/// </summary>
public static class AttentionQueueClassifier
{
    /// <summary>Instalação OFFLINE (DOWN, ver <c>InstallationHealthClassifier.DownThreshold</c>) por mais que isto vira CRITICAL — abaixo, HIGH.</summary>
    public static readonly TimeSpan InstallationOfflineCriticalThreshold = TimeSpan.FromMinutes(60);

    /// <summary>Convite expirado há mais que isto vira HIGH (o dono segue sem acesso há mais de uma semana) — abaixo, MEDIUM.</summary>
    public static readonly TimeSpan InviteExpiredHighThreshold = TimeSpan.FromDays(7);

    /// <summary>Abaixo deste tempo em PROVISIONED/INSTALLING, o fluxo normal de implantação ainda não conta como "parado" — não entra na fila.</summary>
    public static readonly TimeSpan ProvisioningStalledMinimumThreshold = TimeSpan.FromHours(4);

    /// <summary>Acima deste tempo parado, HIGH — abaixo (mas acima do mínimo), MEDIUM.</summary>
    public static readonly TimeSpan ProvisioningStalledHighThreshold = TimeSpan.FromHours(24);

    /// <summary>Acima deste tempo parado, CRITICAL.</summary>
    public static readonly TimeSpan ProvisioningStalledCriticalThreshold = TimeSpan.FromHours(72);

    public static AttentionSeverity ClassifyInstallationOffline(TimeSpan offlineFor) =>
        offlineFor >= InstallationOfflineCriticalThreshold ? AttentionSeverity.Critical : AttentionSeverity.High;

    /// <summary>Instalação DEGRADED nunca é mais que LOW — só OFFLINE prolongado justifica CRITICAL/HIGH.</summary>
    public static AttentionSeverity ClassifyInstallationDegraded() => AttentionSeverity.Low;

    public static AttentionSeverity ClassifyInviteExpired(TimeSpan expiredFor) =>
        expiredFor >= InviteExpiredHighThreshold ? AttentionSeverity.High : AttentionSeverity.Medium;

    /// <summary><c>null</c> quando <paramref name="stalledFor"/> ainda não cruzou <see cref="ProvisioningStalledMinimumThreshold"/> — o chamador não deve reportar o item.</summary>
    public static AttentionSeverity? ClassifyProvisioningStalled(TimeSpan stalledFor)
    {
        if (stalledFor < ProvisioningStalledMinimumThreshold)
            return null;

        if (stalledFor >= ProvisioningStalledCriticalThreshold)
            return AttentionSeverity.Critical;

        return stalledFor >= ProvisioningStalledHighThreshold ? AttentionSeverity.High : AttentionSeverity.Medium;
    }

    public static string ReasonForInstallationOffline(TimeSpan offlineFor) =>
        $"Sem contato há {FormatDuration(offlineFor)}";

    public static string ReasonForInstallationDegraded(TimeSpan degradedFor) =>
        $"Sincronização atrasada há {FormatDuration(degradedFor)}";

    public static string ReasonForInviteExpired(TimeSpan expiredFor) =>
        $"Convite expirado há {FormatDuration(expiredFor)}, proprietário ainda sem acesso";

    public static string ReasonForProvisioningStalled(string statusLabel, TimeSpan stalledFor) =>
        $"Provisionamento parado em {statusLabel} há {FormatDuration(stalledFor)}";

    /// <summary>Duração curta em pt-BR ("18 min", "2 h", "3 dias") — mesma forma de <c>installations-panel-page.tsx#formatDuration</c> no frontend, reescrita no backend para compor o texto substantivo do motivo (nunca só um número cru).</summary>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;

        var totalMinutes = (int)duration.TotalMinutes;
        if (totalMinutes < 60)
            return $"{Math.Max(totalMinutes, 1)} min";

        var totalHours = (int)duration.TotalHours;
        if (totalHours < 24)
            return $"{totalHours} h";

        var totalDays = (int)duration.TotalDays;
        return totalDays == 1 ? "1 dia" : $"{totalDays.ToString(CultureInfo.InvariantCulture)} dias";
    }
}
