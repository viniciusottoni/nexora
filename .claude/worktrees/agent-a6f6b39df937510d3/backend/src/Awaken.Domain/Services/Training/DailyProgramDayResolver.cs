namespace Awaken.Domain.Services.Training;

/// US-238: resolve, de forma 100% determinística (RN-008), qual é o dia
/// (letra) do programa que o usuário deve treinar hoje — o sucessor cíclico
/// do último dia efetivamente concluído no mesmo programa (RN-001). Serviço
/// de domínio puro, sem I/O: quem chama já buscou a sequência de dias
/// (US-237) e o último dayKey concluído (US-062).
public static class DailyProgramDayResolver
{
    /// <param name="days">Sequência ordenada de dayKeys do split map (US-237), ex.: ["A","B","C"].</param>
    /// <param name="lastCompletedDayKey">
    /// DayKey do último Quest concluído no mesmo programa, ou null se não há histórico
    /// (primeiro treino — RN-003) ou se o programa foi trocado (RN-004).
    /// </param>
    public static DailyProgramDayResolution Resolve(IReadOnlyList<string> days, string? lastCompletedDayKey)
    {
        if (days.Count == 0)
            throw new InvalidOperationException("Programa sem dias no split map.");

        if (days.Count == 1)
            return new DailyProgramDayResolution(days[0], 1, "full_body_single_day"); // RN-005

        if (lastCompletedDayKey is null)
            return new DailyProgramDayResolution(days[0], 1, "first_workout"); // RN-003

        var lastIndex = days.ToList().FindIndex(d => string.Equals(d, lastCompletedDayKey, StringComparison.Ordinal));
        if (lastIndex < 0)
            return new DailyProgramDayResolution(days[0], 1, "program_changed"); // RN-004

        var nextIndex = (lastIndex + 1) % days.Count;
        return new DailyProgramDayResolution(days[nextIndex], nextIndex + 1, "cyclic_successor"); // RN-001/RN-007
    }
}

public record DailyProgramDayResolution(string DayKey, int DayIndex, string Reason);
