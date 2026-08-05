using System.Text.Json;

namespace Nexora.Application.Onboarding.Support;

/// <summary>
/// Sinais de completude derivados de estado real do tenant (US-141 §3.1: "quanto mais dessa lista
/// for autoatendido, mais barata a operação da Replay"), usados por
/// <c>RecalculateOnboardingStepsCommandHandler</c>. Espelha a convenção de outras policies de
/// leitura de JSONB livre (<c>BusinessDayPolicy</c>, <c>PendingItemsClosePolicy</c>): default seguro
/// quando ausente/malformado, nunca lança.
/// </summary>
public static class OnboardingStepSignals
{
    /// <summary>
    /// Verdadeiro quando um campo JSONB de <c>TenantConfig</c> (<c>Branding</c>/<c>Payments</c>) saiu
    /// do valor-padrão de um tenant recém-provisionado — ou seja, alguém realmente configurou algo,
    /// não apenas "{}" (default de <c>TenantConfig.Create</c>) nem vazio. Não distingue configuração
    /// vinda de template de provisionamento de configuração editada manualmente pelo cliente — ambas
    /// contam como "o passo já tem conteúdo", que é o que US-141 pede (rastrear progresso real, não
    /// origem do dado).
    /// </summary>
    public static bool HasNonDefaultJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Object => document.RootElement.EnumerateObject().MoveNext(),
                JsonValueKind.Array => document.RootElement.EnumerateArray().MoveNext(),
                _ => true,
            };
        }
        catch (JsonException)
        {
            // JSON malformado — trata como "sem conteúdo confiável", mesmo espírito de BusinessDayPolicy.
            return false;
        }
    }
}
