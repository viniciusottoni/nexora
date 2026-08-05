namespace Nexora.Application.Onboarding.Support;

/// <summary>
/// Dias úteis (segunda a sexta) já decorridos entre o início da implantação e agora (US-141 §7
/// <c>elapsedBusinessDays</c>, §11 "meta ≤ 5 dias úteis").
/// </summary>
/// <remarks>
/// [SIMPLIFICAÇÃO DOCUMENTADA] Isto NÃO é o "dia operacional" de
/// <c>Nexora.Application.Catalog.Availability.BusinessDayPolicy</c> (virada configurável às 5h,
/// usada para decidir retorno automático de disponibilidade de produto) — é uma contagem de
/// calendário corrido (segunda a sexta, sem feriados) para medir a métrica de implantação. Os dois
/// conceitos compartilham o nome "dia útil/operacional" na documentação de negócio, mas resolvem
/// problemas diferentes; não force um a virar o outro. Feriados nacionais/municipais não são
/// descontados (nenhuma fonte de calendário de feriados existe hoje na solution) — mesma classe de
/// simplificação já aceita em <c>BusinessDayPolicy</c> para fuso horário por tenant. Documentar e
/// seguir, em vez de bloquear a história por uma pendência de escopo maior.
/// </remarks>
public static class OnboardingElapsedBusinessDays
{
    /// <summary>
    /// Conta os dias úteis (segunda a sexta) já FECHADOS entre <paramref name="startedAt"/> (data,
    /// UTC) e <paramref name="now"/> (data, UTC) — o dia corrente nunca conta como "decorrido" (ainda
    /// não terminou). Ex.: começou numa segunda, agora é a própria segunda → 0; agora é terça → 1
    /// (a segunda inteira já passou); agora é sábado da mesma semana → 4 (seg/ter/qua/qui, sexta
    /// ainda em curso... não, sexta também já fechou se agora é sábado — ver testes para os casos
    /// de contorno de fim de semana).
    /// </summary>
    public static int Calculate(DateTimeOffset startedAt, DateTimeOffset now)
    {
        var start = startedAt.UtcDateTime.Date;
        var end = now.UtcDateTime.Date;

        if (end <= start)
        {
            return 0;
        }

        var count = 0;
        for (var day = start; day < end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                count++;
            }
        }

        return count;
    }
}
