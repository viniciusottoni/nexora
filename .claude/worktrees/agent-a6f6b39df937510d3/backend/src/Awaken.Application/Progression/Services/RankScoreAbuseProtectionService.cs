namespace Awaken.Application.Progression.Services;

/// US-155: avalia contexto de execução e retorna multiplicador externo para ganho de RankScore.
/// Multiplier 0.0 = sem ganho; 0.5 = ganho reduzido; 1.0 = ganho pleno.
public static class RankScoreAbuseProtectionService
{
    public record AbuseEvaluation(decimal Multiplier, bool AbuseSuspected);

    /// US-155 RN-003: dor forte → sem RankScore.
    /// US-155 RN-004: conclusão parcial (< 50% dos sets) → ganho reduzido.
    public static AbuseEvaluation Evaluate(bool strongPainReported, int setsCompleted, int totalSets)
    {
        if (strongPainReported)
            return new AbuseEvaluation(0.0m, AbuseSuspected: true);

        var completionRatio = totalSets > 0 ? (decimal)setsCompleted / totalSets : 0m;
        if (completionRatio < 0.5m)
            return new AbuseEvaluation(0.5m, AbuseSuspected: true);

        return new AbuseEvaluation(1.0m, AbuseSuspected: false);
    }
}
