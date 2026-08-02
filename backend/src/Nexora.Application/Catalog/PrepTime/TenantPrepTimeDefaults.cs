using System.Text.Json;

namespace Nexora.Application.Catalog.PrepTime;

/// <summary>
/// US-016 — resolve o limiar de atenção/crítico PADRÃO DO TENANT, usado quando uma variação não
/// define o próprio (<c>ProductVariant.WarnMinutes</c>/<c>CriticalMinutes</c> nulos — RN "Limiar
/// herdado do tenant" do documento da US, cenário Gherkin "Limiar herdado do tenant").
/// </summary>
/// <remarks>
/// [PENDÊNCIA] Ainda não existe nenhuma tela/rota que grave um padrão de limiar por tenant.
/// <c>TenantConfig.Thresholds</c> (<c>Nexora.Domain.Platform.TenantConfig</c>) já existe como
/// JSONB livre para exatamente esse propósito ("TODO: value object tipado quando o formato de
/// thresholds for definido" — comentário original daquele campo), então esta classe já lê duas
/// chaves específicas (<c>prepWarnMinutes</c>/<c>prepCriticalMinutes</c>) quando existirem e,
/// como fallback, as chaves já provisionadas (<c>orderWarnMinutes</c>/<c>orderCriticalMinutes</c>).
/// Isso mantém um único local de configuração e faz o padrão PIZZERIA (12/18 minutos) valer desde
/// o primeiro acesso.
/// </remarks>
public static class TenantPrepTimeDefaults
{
    public const short DefaultWarnMinutes = 12;
    public const short DefaultCriticalMinutes = 18;

    private const string WarnKey = "prepWarnMinutes";
    private const string CriticalKey = "prepCriticalMinutes";
    private const string ProvisionedWarnKey = "orderWarnMinutes";
    private const string ProvisionedCriticalKey = "orderCriticalMinutes";

    /// <summary>
    /// Lê <paramref name="thresholdsJson"/> (campo <c>TenantConfig.Thresholds</c>) buscando as
    /// duas chaves de convenção documentadas na classe; usa as constantes padrão para qualquer
    /// chave ausente, JSON vazio/nulo, ou JSON malformado (nunca lança — herança de limiar não
    /// pode derrubar a consulta de análise).
    /// </summary>
    public static (short WarnMinutes, short CriticalMinutes) Resolve(string? thresholdsJson)
    {
        if (string.IsNullOrWhiteSpace(thresholdsJson))
            return (DefaultWarnMinutes, DefaultCriticalMinutes);

        try
        {
            using var document = JsonDocument.Parse(thresholdsJson);
            var root = document.RootElement;

            var warn = ReadPositiveInt16(root, WarnKey)
                ?? ReadPositiveInt16(root, ProvisionedWarnKey)
                ?? DefaultWarnMinutes;

            var critical = ReadPositiveInt16(root, CriticalKey)
                ?? ReadPositiveInt16(root, ProvisionedCriticalKey)
                ?? DefaultCriticalMinutes;

            return (warn, critical);
        }
        catch (JsonException)
        {
            return (DefaultWarnMinutes, DefaultCriticalMinutes);
        }
    }

    private static short? ReadPositiveInt16(JsonElement root, string key)
    {
        return root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(key, out var element)
            && element.TryGetInt16(out var value)
            && value > 0
                ? value
                : null;
    }
}
