using System.Text.Json;

namespace Nexora.Application.Auth.Shared;

/// <summary>
/// Lê <c>sessionInactivityMinutes</c> de <c>TenantConfig.Operation</c> (ADR-032, JSONB livre) — a
/// MESMA chave que <c>Nexora.Domain.Provisioning.ProvisioningTemplates</c> já grava com valor 30
/// para o template "PIZZERIA" (não inventa uma chave nova: US-004, gap "encerramento de sessão
/// inativa configurável" só precisava de quem a LÊ; quem a escreve no provisionamento já existia).
/// Usada por <see cref="IAuthSessionActivityGuard"/> para decidir se uma sessão sem atividade
/// recente ainda é válida.
/// </summary>
public static class SessionInactivityPolicy
{
    /// <summary>
    /// Mesmo valor do template de provisionamento "PIZZERIA" — usado quando o tenant não tem
    /// <c>TenantConfig</c> (não deveria acontecer em produção, mas os testes de integração desta
    /// solution frequentemente semeiam tenant sem config completo) ou quando a chave está ausente
    /// do JSON (tenant provisionado antes desta chave existir).
    /// </summary>
    public const int DefaultMinutes = 30;

    private const string Key = "sessionInactivityMinutes";

    public static int ResolveMinutes(string? operationJson)
    {
        if (string.IsNullOrWhiteSpace(operationJson))
        {
            return DefaultMinutes;
        }

        try
        {
            using var document = JsonDocument.Parse(operationJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(Key, out var value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var minutes) &&
                minutes > 0)
            {
                return minutes;
            }
        }
        catch (JsonException)
        {
            // Operation malformado — cai no default seguro em vez de travar a requisição por um
            // problema de configuração que não é culpa de quem está autenticado.
        }

        return DefaultMinutes;
    }
}
