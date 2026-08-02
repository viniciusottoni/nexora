using System.Text.Json;

namespace Nexora.Application.Catalog.Availability;

/// <summary>
/// Calcula a virada do dia operacional ("business day", ADR-018) para decidir o retorno
/// automático de um produto marcado indisponível (US-015 §3.1/§15, cenário "Retorno automático no
/// novo dia operacional"). Espelha <c>Nexora.Application.Auth.Shared.SessionInactivityPolicy</c> —
/// mesma convenção de ler uma chave configurável de <c>TenantConfig.Operation</c> (JSONB livre,
/// ADR-032) com um default seguro quando ausente/malformado.
///
/// <para>
/// [HIPÓTESE/simplificação documentada] ADR-018 fala de virada "configurável por tenant, padrão
/// 5h" mas não define ainda em qual fuso horário — esta solution não modela fuso por tenant hoje
/// (nenhum campo de timezone em <c>Tenant</c>/<c>TenantConfig</c>, confirmado por busca no
/// código). Por isso <see cref="BusinessDayStartHourUtc"/> é tratado como hora UTC diretamente,
/// não hora local convertida — mesma limitação seria enfrentada por qualquer outro módulo que
/// precisasse de "dia operacional" antes de uma US dedicada a fuso horário por tenant. Documentar
/// e seguir, em vez de bloquear esta história por uma pendência de escopo maior.
/// </para>
/// </summary>
public static class BusinessDayPolicy
{
    /// <summary>Mesmo valor citado pelo ADR-018 ("padrão 5h") — usado quando o tenant não configurou <c>businessDayStartHour</c>.</summary>
    public const int DefaultStartHourUtc = 5;

    private const string Key = "businessDayStartHour";

    /// <summary>Lê <c>businessDayStartHour</c> (0-23) de <c>TenantConfig.Operation</c>; cai no default seguro se ausente/inválido/malformado.</summary>
    public static int ResolveStartHourUtc(string? operationJson)
    {
        if (string.IsNullOrWhiteSpace(operationJson))
        {
            return DefaultStartHourUtc;
        }

        try
        {
            using var document = JsonDocument.Parse(operationJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(Key, out var value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var hour) &&
                hour is >= 0 and <= 23)
            {
                return hour;
            }
        }
        catch (JsonException)
        {
            // Operation malformado — cai no default seguro, mesmo espírito de SessionInactivityPolicy.
        }

        return DefaultStartHourUtc;
    }

    /// <summary>
    /// Início (UTC) do dia operacional corrente às <paramref name="nowUtc"/> — a última ocorrência
    /// de <paramref name="startHourUtc"/> que não é depois de <paramref name="nowUtc"/>. Ex.: virada
    /// às 5h, agora 03:00 de 10/03 → dia operacional começou às 05:00 de 09/03 (o "dia de ontem"
    /// ainda não virou); agora 08:00 de 10/03 → começou às 05:00 de 10/03.
    /// </summary>
    public static DateTimeOffset CurrentBusinessDayStart(DateTimeOffset nowUtc, int startHourUtc)
    {
        var todayBoundary = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, startHourUtc, 0, 0, TimeSpan.Zero);
        return nowUtc >= todayBoundary ? todayBoundary : todayBoundary.AddDays(-1);
    }

    /// <summary>
    /// Verdadeiro quando <paramref name="unavailableSince"/> ficou para trás de uma virada de dia
    /// operacional em relação a <paramref name="nowUtc"/> — ou seja, o produto foi marcado
    /// indisponível num dia operacional anterior ao atual, e por isso deve voltar a ficar
    /// disponível (US-015 §3.1, cenário "Retorno automático no novo dia operacional"). Produto
    /// marcado indisponível DENTRO do dia operacional corrente (ainda não virou) permanece
    /// indisponível.
    /// </summary>
    public static bool HasCrossedBusinessDayBoundary(DateTimeOffset unavailableSince, DateTimeOffset nowUtc, int startHourUtc) =>
        unavailableSince < CurrentBusinessDayStart(nowUtc, startHourUtc);
}
