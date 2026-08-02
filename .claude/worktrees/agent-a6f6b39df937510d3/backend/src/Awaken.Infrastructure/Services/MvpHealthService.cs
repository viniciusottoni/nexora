using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.MvpHealth;
using Awaken.Domain.Repositories;
using Awaken.Shared.Admin;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Services;

/// <summary>
/// US-216: agrega sinais de saúde de todos os domínios operacionais do MVP em uma visão consolidada.
///
/// RN-003: cada domínio sem dados reais disponíveis reporta "no_data", nunca "healthy".
/// Resiliência: exceção em um domínio não impede os demais — aquele domínio é reportado como "no_data".
/// ADR-015: nunca expõe credenciais, tokens, connection strings ou payloads sensíveis nos campos retornados.
/// </summary>
public class MvpHealthService(
    IReadinessCheckService readinessCheckService,
    IPerformanceMetricsService performanceMetricsService,
    IJobMonitoringService jobMonitoringService,
    IAdminSubscriptionDiagnosticsRepository subscriptionDiagnosticsRepository,
    ISecurityAlertRepository securityAlertRepository,
    IDateTimeService dateTimeService,
    ILogger<MvpHealthService> logger)
    : IMvpHealthService
{
    public async Task<MvpHealthStatusResponse> GetMvpHealthAsync(CancellationToken cancellationToken = default)
    {
        var now = dateTimeService.UtcNow;

        var domains = new List<DomainCardResponse>();

        domains.Add(await BuildSecurityDomainAsync(now, cancellationToken));
        domains.Add(await BuildSubscriptionsDomainAsync(now, cancellationToken));
        domains.Add(await BuildConfigurationDomainAsync(now, cancellationToken));
        domains.Add(await BuildPerformanceDomainAsync(now, cancellationToken));
        domains.Add(await BuildJobsDomainAsync(now, cancellationToken));
        domains.Add(BuildMediaCdnDomain(now));
        domains.Add(BuildObservabilityDomain(now));
        domains.Add(BuildLoadTestDomain(now));

        var p0Blockers = domains
            .Where(d => d.Status == DomainHealthStatus.Critical && d.Description is not null)
            .Select(d => $"[{d.Label}] {d.Description}")
            .ToList();

        var overallStatus = DomainHealthStatus.Aggregate(domains.Select(d => d.Status));

        return new MvpHealthStatusResponse(overallStatus, domains, p0Blockers, now);
    }

    // ── Security ─────────────────────────────────────────────────────────────

    private async Task<DomainCardResponse> BuildSecurityDomainAsync(DateTime now, CancellationToken ct)
    {
        const string key = "security";
        const string label = "Segurança";
        const string detailUrl = "/admin/security";

        try
        {
            var counts = await securityAlertRepository.CountOpenBySeverityAsync(ct);

            var criticalCount = counts
                .Where(c => c.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase))
                .Sum(c => c.Count);

            var attentionCount = counts
                .Where(c => c.Severity.Equals("high", StringComparison.OrdinalIgnoreCase)
                         || c.Severity.Equals("medium", StringComparison.OrdinalIgnoreCase))
                .Sum(c => c.Count);

            if (criticalCount > 0)
            {
                return new DomainCardResponse(key, label, DomainHealthStatus.Critical,
                    $"{criticalCount} alerta(s) crítico(s) em aberto.", detailUrl, now);
            }

            if (attentionCount > 0)
            {
                return new DomainCardResponse(key, label, DomainHealthStatus.Attention,
                    $"{attentionCount} alerta(s) de atenção em aberto.", detailUrl, now);
            }

            return new DomainCardResponse(key, label, DomainHealthStatus.Healthy,
                "Nenhum alerta de segurança em aberto.", detailUrl, now);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "US-216: falha ao coletar sinais de segurança.");
            return new DomainCardResponse(key, label, DomainHealthStatus.NoData,
                "Não foi possível coletar sinais de segurança.", detailUrl, now);
        }
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────

    private async Task<DomainCardResponse> BuildSubscriptionsDomainAsync(DateTime now, CancellationToken ct)
    {
        const string key = "subscriptions";
        const string label = "Assinaturas/IAP";
        const string detailUrl = "/admin/subscriptions";

        try
        {
            var counts = await subscriptionDiagnosticsRepository.GetCountsAsync(null, null, 30, ct);

            if (counts.FailedCount > 0 || counts.PendingGrantsCount > 0)
            {
                var parts = new List<string>();
                if (counts.FailedCount > 0)
                    parts.Add($"{counts.FailedCount} falha(s)");
                if (counts.PendingGrantsCount > 0)
                    parts.Add($"{counts.PendingGrantsCount} concessão(ões) pendente(s)");

                return new DomainCardResponse(key, label, DomainHealthStatus.Attention,
                    string.Join(", ", parts) + " detectada(s).", detailUrl, now);
            }

            return new DomainCardResponse(key, label, DomainHealthStatus.Healthy,
                "Sem falhas ou concessões pendentes.", detailUrl, now);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "US-216: falha ao coletar sinais de assinaturas.");
            return new DomainCardResponse(key, label, DomainHealthStatus.NoData,
                "Não foi possível coletar sinais de assinaturas/IAP.", detailUrl, now);
        }
    }

    // ── Configuration ─────────────────────────────────────────────────────────

    private async Task<DomainCardResponse> BuildConfigurationDomainAsync(DateTime now, CancellationToken ct)
    {
        const string key = "configuration";
        const string label = "Configuração";
        const string detailUrl = "/admin/readiness";

        try
        {
            var readiness = await readinessCheckService.GetReadinessStatusAsync(ct);

            var hasBlocker = readiness.Environments.Any(e => e.HasBlocker);
            var hasAttention = readiness.Environments.Any(e =>
                e.OverallStatus == DomainHealthStatus.Attention);

            if (hasBlocker)
            {
                return new DomainCardResponse(key, label, DomainHealthStatus.Critical,
                    "Configuração obrigatória ausente ou inválida em produção.", detailUrl, now);
            }

            if (hasAttention)
            {
                return new DomainCardResponse(key, label, DomainHealthStatus.Attention,
                    "Itens de configuração requerem atenção.", detailUrl, now);
            }

            return new DomainCardResponse(key, label, DomainHealthStatus.Healthy,
                "Configuração obrigatória presente em todos os ambientes.", detailUrl, now);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "US-216: falha ao coletar sinais de configuração.");
            return new DomainCardResponse(key, label, DomainHealthStatus.NoData,
                "Não foi possível coletar sinais de configuração.", detailUrl, now);
        }
    }

    // ── Performance (Database + Redis) ────────────────────────────────────────

    private async Task<DomainCardResponse> BuildPerformanceDomainAsync(DateTime now, CancellationToken ct)
    {
        const string key = "performance";
        const string label = "Banco / Redis";
        const string detailUrl = "/admin/performance";

        try
        {
            var dbHealth = await performanceMetricsService.GetDatabaseHealthAsync(ct);
            var redisHealth = await performanceMetricsService.GetRedisHealthAsync(ct);

            var worstStatus = DomainHealthStatus.Aggregate([dbHealth.Status, redisHealth.Status]);

            string? description = worstStatus switch
            {
                DomainHealthStatus.Critical => "Banco de dados ou Redis indisponível.",
                DomainHealthStatus.Attention => "Banco de dados ou Redis com latência elevada.",
                DomainHealthStatus.NoData    => "Sem dados de saúde de banco/Redis disponíveis.",
                _                            => "Banco de dados e Redis operacionais.",
            };

            return new DomainCardResponse(key, label, worstStatus, description, detailUrl, now);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "US-216: falha ao coletar sinais de performance (banco/Redis).");
            return new DomainCardResponse(key, label, DomainHealthStatus.NoData,
                "Não foi possível coletar sinais de banco/Redis.", detailUrl, now);
        }
    }

    // ── Jobs / Routines ────────────────────────────────────────────────────────

    private async Task<DomainCardResponse> BuildJobsDomainAsync(DateTime now, CancellationToken ct)
    {
        const string key = "jobs";
        const string label = "Jobs / Rotinas";
        const string detailUrl = "/admin/routines";

        try
        {
            var overview = await jobMonitoringService.GetRoutinesOverviewAsync(ct);

            var aggregated = DomainHealthStatus.Aggregate(
            [
                overview.WorkersStatus,
                overview.RoutinesStatus,
                overview.QueuesStatus,
            ]);

            string? description = aggregated switch
            {
                DomainHealthStatus.Critical => "Workers ou rotinas com falha crítica.",
                DomainHealthStatus.Attention => "Workers ou rotinas requerem atenção.",
                DomainHealthStatus.NoData    => "Sem dados de jobs/rotinas disponíveis.",
                _                            => "Workers e rotinas operacionais.",
            };

            return new DomainCardResponse(key, label, aggregated, description, detailUrl, now);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "US-216: falha ao coletar sinais de jobs/rotinas.");
            return new DomainCardResponse(key, label, DomainHealthStatus.NoData,
                "Não foi possível coletar sinais de jobs/rotinas.", detailUrl, now);
        }
    }

    // ── Static NoData domains ─────────────────────────────────────────────────

    private static DomainCardResponse BuildMediaCdnDomain(DateTime now) =>
        new("media_cdn", "Mídia / CDN", DomainHealthStatus.NoData,
            "Diagnóstico sob demanda. Acesse Mídia/CDN para executar.",
            "/admin/media", now);

    private static DomainCardResponse BuildObservabilityDomain(DateTime now) =>
        new("observability", "Observabilidade", DomainHealthStatus.NoData,
            "Sem integração de observabilidade configurada no MVP.",
            null, now);

    private static DomainCardResponse BuildLoadTestDomain(DateTime now) =>
        new("load_test", "Teste de Carga", DomainHealthStatus.NoData,
            "Teste de carga não integrado automaticamente no MVP.",
            null, now);
}
