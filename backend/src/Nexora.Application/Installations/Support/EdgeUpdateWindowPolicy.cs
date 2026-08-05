using System.Text.Json;

namespace Nexora.Application.Installations.Support;

/// <summary>
/// Decide se o instante corrente está dentro da janela de atualização configurável por instalação
/// (US-146 §7 pseudocódigo "dentro da janela"; doc. 01 comenta <c>tenant_config.maintenance</c>
/// como "janela de atualização (ADR-019)"). Espelha <c>Nexora.Application.Catalog.Availability.BusinessDayPolicy</c>
/// — mesma convenção de ler uma chave configurável de JSONB livre (ADR-032) com default seguro
/// quando ausente/malformado.
/// </summary>
/// <remarks>
/// [HIPÓTESE/simplificação documentada] Mesma limitação já registrada em <c>BusinessDayPolicy</c>:
/// esta solution não modela fuso horário por tenant hoje (nenhum campo de timezone em
/// <c>Tenant</c>/<c>TenantConfig</c> — só em <c>Store</c>, não lido aqui porque o ciclo de
/// atualização roda sem HTTP request/tenant context de negócio, só a única instalação local do
/// edge). <see cref="UpdateWindowStartHourUtc"/>/<see cref="UpdateWindowEndHourUtc"/> são tratados
/// como hora UTC diretamente, não hora local convertida. Formato do JSON assumido (não documentado
/// em nenhum outro lugar do pacote — decisão desta história, registrada aqui para quem for ler o
/// DDL depois): <c>{ "updateWindowStartHour": 4, "updateWindowEndHour": 6 }</c>.
/// </remarks>
public static class EdgeUpdateWindowPolicy
{
    /// <summary>US-146 §4, cenário "Atualização na janela configurada" — mesmo exemplo do Gherkin (4h-6h).</summary>
    public const int DefaultStartHourUtc = 4;
    public const int DefaultEndHourUtc = 6;

    private const string StartKey = "updateWindowStartHour";
    private const string EndKey = "updateWindowEndHour";

    public static (int StartHourUtc, int EndHourUtc) ResolveWindow(string? maintenanceJson)
    {
        if (string.IsNullOrWhiteSpace(maintenanceJson))
        {
            return (DefaultStartHourUtc, DefaultEndHourUtc);
        }

        try
        {
            using var document = JsonDocument.Parse(maintenanceJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (DefaultStartHourUtc, DefaultEndHourUtc);
            }

            var start = ReadHour(document.RootElement, StartKey, DefaultStartHourUtc);
            var end = ReadHour(document.RootElement, EndKey, DefaultEndHourUtc);
            return (start, end);
        }
        catch (JsonException)
        {
            // Maintenance malformado — cai no default seguro, mesmo espírito de BusinessDayPolicy.
            return (DefaultStartHourUtc, DefaultEndHourUtc);
        }
    }

    /// <summary>
    /// Verdadeiro quando <paramref name="nowUtc"/> cai dentro de [<paramref name="startHourUtc"/>,
    /// <paramref name="endHourUtc"/>) — suporta janela que atravessa a meia-noite (ex.: 22h-2h),
    /// comparando só a HORA do instante corrente (minutos/segundos irrelevantes para a granularidade
    /// desta janela).
    /// </summary>
    public static bool IsWithinWindow(DateTimeOffset nowUtc, int startHourUtc, int endHourUtc)
    {
        var hour = nowUtc.Hour;

        return startHourUtc <= endHourUtc
            ? hour >= startHourUtc && hour < endHourUtc
            : hour >= startHourUtc || hour < endHourUtc; // janela atravessa a meia-noite.
    }

    private static int ReadHour(JsonElement root, string key, int fallback)
    {
        if (root.TryGetProperty(key, out var value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var hour) &&
            hour is >= 0 and <= 23)
        {
            return hour;
        }

        return fallback;
    }
}
