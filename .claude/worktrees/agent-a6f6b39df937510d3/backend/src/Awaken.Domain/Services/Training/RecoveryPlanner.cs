using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Training;

namespace Awaken.Domain.Services.Training;

/// US-239: serviço de domínio puro (determinístico, sem I/O) que aplica as
/// tabelas científicas de recuperação muscular (janela por intensidade,
/// teto de volume semanal por nível efetivo) — seções 6.1/6.2/6.3 da US-239.
/// Não escolhe exercícios (US-240) nem prescreve séries/reps (US-153); só
/// produz restrições/moduladores que essas USes consomem.
public static class RecoveryPlanner
{
    // Seção 6.1 da US-239: janela mínima por intensidade da última sessão.
    private static readonly Dictionary<string, int> RecoveryWindowHours = new(StringComparer.Ordinal)
    {
        ["light"] = 24,
        ["moderate"] = 48,
        ["heavy"] = 72,
    };

    // Seção 6.2 da US-239: teto de séries semanais recuperáveis por nível efetivo.
    private static readonly Dictionary<string, (int Min, int Max, int PerSessionCap)> WeeklyVolumeByLevel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sedentary"] = (6, 10, 2),
        ["beginner"] = (8, 12, 3),
        ["intermediate"] = (12, 16, 4),
        ["advanced"] = (14, 20, 5),
    };

    /// RN-001/RN 10.4: sem histórico prévio (ou após folga longa), o grupo
    /// está recuperado por padrão — sem penalidade.
    public static MuscleRecoveryStatus StatusFor(MuscleRecoveryState? state, DateTime utcNow)
    {
        if (state is null)
            return MuscleRecoveryStatus.Recovered;

        var windowHours = RecoveryWindowHours.GetValueOrDefault(state.LastIntensity, 48);
        var hoursSince = (utcNow - state.LastTrainedAtUtc).TotalHours;

        if (hoursSince >= windowHours)
            return MuscleRecoveryStatus.Recovered;

        // Muito abaixo da janela (< 50% do tempo necessário) e volume semanal já alto = fadigado.
        return hoursSince < windowHours * 0.5
            ? MuscleRecoveryStatus.Fatigued
            : MuscleRecoveryStatus.Recovering;
    }

    /// Seção 6.3 da US-239: cap de volume e ajuste de RPE por status (RN-003).
    public static (decimal VolumeCapFactor, int RpeCapDelta) ModulationFor(MuscleRecoveryStatus status) => status switch
    {
        MuscleRecoveryStatus.Recovered => (1.0m, 0),
        MuscleRecoveryStatus.Recovering => (0.5m, -1),
        MuscleRecoveryStatus.Fatigued => (0.25m, -2),
        _ => (1.0m, 0),
    };

    public static int WeeklySetCapFor(string effectiveExperienceLevel) =>
        WeeklyVolumeByLevel.TryGetValue(effectiveExperienceLevel, out var range) ? range.Max : WeeklyVolumeByLevel["sedentary"].Max;

    public static int PerSessionSetCapFor(string effectiveExperienceLevel) =>
        WeeklyVolumeByLevel.TryGetValue(effectiveExperienceLevel, out var range) ? range.PerSessionCap : WeeklyVolumeByLevel["sedentary"].PerSessionCap;

    /// US-239 §13: intensidade da sessão inferida a partir do catálogo do
    /// exercício concluído, usada por <see cref="Awaken.Application.Quests.Commands.CompleteQuest.CompleteQuestCommandHandler"/>
    /// para registrar o <see cref="MuscleRecoveryState"/> dos grupos trabalhados.
    public static string IntensityFor(ExerciseCatalog exercise)
    {
        if (exercise.ImpactLevel >= 4 || exercise.DifficultyRank >= 3)
            return "heavy";

        return exercise.DifficultyRank == 0 ? "light" : "moderate";
    }
}

public enum MuscleRecoveryStatus { Recovered, Recovering, Fatigued }
