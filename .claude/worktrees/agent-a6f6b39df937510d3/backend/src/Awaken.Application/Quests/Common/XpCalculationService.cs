namespace Awaken.Application.Quests.Common;

/// US-065: cálculo de XP por exercício, proporcional ao grau de conclusão
/// e respeitando a restrição de segurança com dor forte.
public static class XpCalculationService
{
    /// <param name="xpReward">XP base do exercício (capturado no Start da quest).</param>
    /// <param name="totalSets">Total de séries prescritas.</param>
    /// <param name="setsCompleted">Séries efetivamente concluídas.</param>
    /// <param name="strongPainReported">True quando o usuário indicou dor forte (RN-003).</param>
    /// <returns>XP a ser concedido, proporcional à conclusão, sem bônus por dor.</returns>
    public static long CalculateExerciseXp(
        long xpReward,
        int totalSets,
        int setsCompleted,
        bool strongPainReported)
    {
        if (totalSets <= 0 || setsCompleted <= 0) return 0;

        var effective = Math.Min(setsCompleted, totalSets);
        var ratio = (double)effective / totalSets;
        var calculated = (long)Math.Round(xpReward * ratio);

        // RN-003: dor forte nunca dá bônus extra. O valor proporcional já é o teto.
        return calculated;
    }

    /// Média de XP por exercício de uma quest — referência para penalidade (US-132).
    public static long AverageQuestXp(IReadOnlyList<long> exerciseXpValues)
    {
        if (exerciseXpValues.Count == 0) return 0;
        return (long)Math.Round(exerciseXpValues.Average());
    }
}
