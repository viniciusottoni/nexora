using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Readiness;
using Awaken.Shared.Admin;
using Microsoft.Extensions.Configuration;

namespace Awaken.Infrastructure.Services;

/// <summary>
/// US-218: avalia readiness de configuração obrigatória, build mobile e CI por ambiente.
///
/// RN-001: nunca expõe valores reais de configuração — apenas presença/forma/status.
/// RN-002: produção com configuração obrigatória ausente é reportada como bloqueador (Critical).
/// RN-003: build mobile release com configuração de teste é Critical; sem como inferir automaticamente
///         no MVP, retorna NoData com descrição explicando (não inventa dado).
/// RN-004: histórico de CI — sem integração real com pipeline no repositório, retorna NoData honesto.
/// RN-005: ambiente sem telemetria/dados aparece como NoData (nunca Healthy por omissão).
///
/// A lógica de presença/forma reaproveita os MESMOS critérios de
/// Awaken.Api.Configuration.StartupConfigurationValidator (JWT >= 32 chars, CORS origins, Google
/// ClientId, connection strings), reimplementados aqui pois aquele validador é fora de escopo desta US.
/// </summary>
public class ReadinessCheckService(IConfiguration configuration, IDateTimeService dateTimeService)
    : IReadinessCheckService
{
    private const string CategoryConfiguration = "configuration";
    private const string CategoryBuild = "build";
    private const string CategorySocialLogin = "social_login";
    private const string CategoryCi = "ci";

    private static readonly string[] PlaceholderPatterns =
        ["CHANGE_THIS", "PLACEHOLDER", "your-", "YOUR_", "example", "TODO", "xxx"];

    private static readonly string[] MonitoredEnvironments = ["dev", "staging", "prod"];

    public Task<ReadinessStatusResponse> GetReadinessStatusAsync(CancellationToken cancellationToken = default)
    {
        var now = dateTimeService.UtcNow;

        var environments = MonitoredEnvironments
            .Select(env => BuildEnvironmentReadiness(env, now))
            .ToList();

        return Task.FromResult(new ReadinessStatusResponse(environments, now));
    }

    private EnvironmentReadinessResponse BuildEnvironmentReadiness(string environment, DateTime now)
    {
        var checks = new List<ReadinessCheckResponse>();

        checks.AddRange(BuildConfigurationChecks(environment, now));
        checks.Add(BuildMobileBuildCheck(environment, now));
        checks.Add(BuildSocialLoginCheck(environment, now));
        checks.Add(BuildCiHistoryCheck(now));

        var overallStatus = DomainHealthStatus.Aggregate(checks.Select(c => c.Status));
        var hasBlocker = checks.Any(c => c.IsBlocker);

        return new EnvironmentReadinessResponse(environment, overallStatus, hasBlocker, checks, now);
    }

    // ── Configuração obrigatória (RN-001, RN-002) ──────────────────────────────

    private List<ReadinessCheckResponse> BuildConfigurationChecks(string environment, DateTime now)
    {
        var isProduction = IsProductionLike(environment);

        return
        [
            EvaluatePresenceCheck(
                name: "JWT Secret",
                category: CategoryConfiguration,
                value: configuration["Jwt:Secret"],
                isValidShape: v => v!.Length >= 32 && !IsPlaceholder(v),
                missingDescription: "Segredo JWT não configurado.",
                invalidDescription: "Segredo JWT presente, mas não atende ao formato mínimo exigido (>= 32 caracteres, sem placeholder).",
                healthyDescription: "Segredo JWT presente e com formato válido.",
                isProduction: isProduction,
                now: now),

            EvaluatePresenceCheck(
                name: "Admin JWT Secret",
                category: CategoryConfiguration,
                value: configuration["AdminJwt:Secret"],
                isValidShape: v => v!.Length >= 32 && !IsPlaceholder(v),
                missingDescription: "Segredo JWT do admin site não configurado.",
                invalidDescription: "Segredo JWT do admin site presente, mas não atende ao formato mínimo exigido (>= 32 caracteres, sem placeholder).",
                healthyDescription: "Segredo JWT do admin site presente e com formato válido.",
                isProduction: isProduction,
                now: now),

            EvaluateCorsCheck(now, isProduction),

            EvaluateGoogleClientIdCheck(now, isProduction),

            EvaluatePresenceCheck(
                name: "Conexão PostgreSQL",
                category: CategoryConfiguration,
                value: configuration.GetConnectionString("PostgreSQL"),
                isValidShape: v => !IsPlaceholder(v!),
                missingDescription: "String de conexão PostgreSQL não configurada.",
                invalidDescription: "String de conexão PostgreSQL presente, mas contém valor de placeholder/teste.",
                healthyDescription: "String de conexão PostgreSQL presente e configurada.",
                isProduction: isProduction,
                now: now),

            EvaluatePresenceCheck(
                name: "Conexão Redis",
                category: CategoryConfiguration,
                value: configuration.GetConnectionString("Redis"),
                isValidShape: v => !IsPlaceholder(v!),
                missingDescription: "String de conexão Redis não configurada.",
                invalidDescription: "String de conexão Redis presente, mas contém valor de placeholder/teste.",
                healthyDescription: "String de conexão Redis presente e configurada.",
                isProduction: isProduction,
                now: now),
        ];
    }

    private ReadinessCheckResponse EvaluatePresenceCheck(
        string name,
        string category,
        string? value,
        Func<string, bool> isValidShape,
        string missingDescription,
        string invalidDescription,
        string healthyDescription,
        bool isProduction,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // RN-002: ausência de configuração obrigatória em produção é bloqueador.
            var status = isProduction ? DomainHealthStatus.Critical : DomainHealthStatus.Attention;
            return new ReadinessCheckResponse(name, category, status, missingDescription, isProduction, now);
        }

        if (!isValidShape(value))
        {
            var status = isProduction ? DomainHealthStatus.Critical : DomainHealthStatus.Attention;
            return new ReadinessCheckResponse(name, category, status, invalidDescription, isProduction, now);
        }

        return new ReadinessCheckResponse(name, category, DomainHealthStatus.Healthy, healthyDescription, false, now);
    }

    private ReadinessCheckResponse EvaluateCorsCheck(DateTime now, bool isProduction)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (origins is null || origins.Length == 0)
        {
            var status = isProduction ? DomainHealthStatus.Critical : DomainHealthStatus.Attention;
            return new ReadinessCheckResponse(
                "CORS Allowed Origins", CategoryConfiguration, status,
                "Nenhuma origem CORS configurada.", isProduction, now);
        }

        return new ReadinessCheckResponse(
            "CORS Allowed Origins", CategoryConfiguration, DomainHealthStatus.Healthy,
            $"{origins.Length} origem(ns) CORS configurada(s).", false, now);
    }

    private ReadinessCheckResponse EvaluateGoogleClientIdCheck(DateTime now, bool isProduction)
    {
        var clientId = configuration["Google:ClientId"];
        var allowedClientIds = configuration.GetSection("Google:AllowedClientIds").Get<string[]>() ?? [];
        var hasAny = !string.IsNullOrWhiteSpace(clientId) || allowedClientIds.Length > 0;

        if (!hasAny)
        {
            // Login social com Google é opcional na forma, mas sem dado configurado fica sem dados.
            return new ReadinessCheckResponse(
                "Google ClientId", CategoryConfiguration, DomainHealthStatus.NoData,
                "Nenhum Google ClientId configurado — login social com Google desabilitado neste ambiente.",
                false, now);
        }

        if (!string.IsNullOrWhiteSpace(clientId) && IsPlaceholder(clientId))
        {
            var status = isProduction ? DomainHealthStatus.Critical : DomainHealthStatus.Attention;
            return new ReadinessCheckResponse(
                "Google ClientId", CategoryConfiguration, status,
                "Google ClientId presente, mas contém valor de placeholder/teste.", isProduction, now);
        }

        return new ReadinessCheckResponse(
            "Google ClientId", CategoryConfiguration, DomainHealthStatus.Healthy,
            "Google ClientId presente e configurado.", false, now);
    }

    // ── Build mobile (RN-003) ───────────────────────────────────────────────────

    private ReadinessCheckResponse BuildMobileBuildCheck(string environment, DateTime now)
    {
        // MVP: não há fonte automática (CI/artefato) que informe ambiente/modo de distribuição/API
        // alvo do build mobile gerado. Reportar NoData honestamente em vez de inferir/simular.
        return new ReadinessCheckResponse(
            "Build Mobile (release vs. configuração)",
            CategoryBuild,
            DomainHealthStatus.NoData,
            "Sem fonte automática de metadados de build mobile (ambiente/distribuição/API de destino) " +
            "neste MVP. Validação manual necessária antes do go-live.",
            false,
            now);
    }

    // ── Login social pronto para o ambiente ─────────────────────────────────────

    private ReadinessCheckResponse BuildSocialLoginCheck(string environment, DateTime now)
    {
        var isProduction = IsProductionLike(environment);
        var clientId = configuration["Google:ClientId"];
        var allowedClientIds = configuration.GetSection("Google:AllowedClientIds").Get<string[]>() ?? [];
        var hasAny = !string.IsNullOrWhiteSpace(clientId) || allowedClientIds.Length > 0;

        if (!hasAny)
        {
            return new ReadinessCheckResponse(
                "Login Social (Google)", CategorySocialLogin, DomainHealthStatus.NoData,
                "Login social com Google não configurado neste ambiente.", false, now);
        }

        var hasPlaceholder = (!string.IsNullOrWhiteSpace(clientId) && IsPlaceholder(clientId))
            || allowedClientIds.Any(IsPlaceholder);

        if (hasPlaceholder)
        {
            var status = isProduction ? DomainHealthStatus.Critical : DomainHealthStatus.Attention;
            return new ReadinessCheckResponse(
                "Login Social (Google)", CategorySocialLogin, status,
                "Login social com Google configurado com valor de placeholder/teste.", isProduction, now);
        }

        return new ReadinessCheckResponse(
            "Login Social (Google)", CategorySocialLogin, DomainHealthStatus.Healthy,
            "Login social com Google pronto para este ambiente.", false, now);
    }

    // ── Histórico do último CI (RN-004) ─────────────────────────────────────────

    private ReadinessCheckResponse BuildCiHistoryCheck(DateTime now)
    {
        // MVP: não existe integração real com o resultado do pipeline de CI (commit, data, status,
        // motivo da falha) persistida no backend. Retornar NoData honesto em vez de simular dado falso.
        return new ReadinessCheckResponse(
            "Último Check de CI",
            CategoryCi,
            DomainHealthStatus.NoData,
            "Sem integração com resultado real de CI (commit/data/status/motivo) neste MVP. " +
            "Consulte o pipeline diretamente até a integração ser implementada.",
            false,
            now);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsProductionLike(string environment) =>
        environment.Equals("prod", StringComparison.OrdinalIgnoreCase)
        || environment.Equals("production", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlaceholder(string value) =>
        PlaceholderPatterns.Any(p => value.Contains(p, StringComparison.OrdinalIgnoreCase));
}
