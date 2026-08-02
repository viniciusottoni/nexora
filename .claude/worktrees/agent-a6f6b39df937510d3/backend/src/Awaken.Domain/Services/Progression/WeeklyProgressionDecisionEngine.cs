using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;

namespace Awaken.Domain.Services.Progression;

/// <summary>
/// US-241 §6.2/6.3/6.6: decide progredir/manter/regredir/deload a partir do
/// sentimento predominante da semana (proxy de RPE, ver <see cref="PerceivedFeelings"/>)
/// e aplica o viés de rank/atributos (RN-007), sempre avançando no máximo 1 eixo
/// por vez (RN-003). Determinístico e puro — não conhece banco nem HTTP.
/// </summary>
public static class WeeklyProgressionDecisionEngine
{
    // RN-003: ordem fixa do único eixo que pode avançar por vez.
    private static readonly string[] AxisCycle = ["reps", "add_set", "harder_variant", "less_rest", "higher_rpe"];
    private const int HighAttributeThreshold = 6; // de 0-10 (US-130) — "alto" para fins de viés (US-241 §6.6).

    public static WeeklyProgressionDecision Decide(WeeklyProgressionDecisionRequest request)
    {
        var dominantFeeling = DominantFeeling(request.RecentFeelings);

        var deloadTriggered =
            request.MesocycleWeekIndex >= WeeklyProgressionState.MaxConsecutiveWeeksBeforeDeload ||
            request.ConsecutiveHardWeeks >= WeeklyProgressionState.MaxConsecutiveHardWeeksBeforeDeload;

        if (deloadTriggered)
        {
            return new WeeklyProgressionDecision(
                Decision: "deload",
                Axis: null,
                VolumeSetsDelta: (int)Math.Round(request.CurrentVolumeSetsDelta * 0.5, MidpointRounding.AwayFromZero),
                RpeDelta: -1,
                RestSecondsDelta: 0,
                ConsecutiveEasyWeeks: 0,
                ConsecutiveHardWeeks: 0,
                DeloadWeek: true,
                AttributeBias: BuildAttributeBias(request.Attributes));
        }

        if (dominantFeeling is null)
        {
            // RN (10.4): dados insuficientes -> mantém o patamar, não progride às cegas.
            return new WeeklyProgressionDecision(
                "hold", null, request.CurrentVolumeSetsDelta, 0, 0,
                0, request.ConsecutiveHardWeeks, false, BuildAttributeBias(request.Attributes));
        }

        if (dominantFeeling == PerceivedFeelings.TooHard)
        {
            var regressedVolume = Math.Max(0, request.CurrentVolumeSetsDelta - 1);
            return new WeeklyProgressionDecision(
                "regress", null, regressedVolume, 0, 0,
                0, request.ConsecutiveHardWeeks + 1, false, BuildAttributeBias(request.Attributes));
        }

        if (dominantFeeling == PerceivedFeelings.JustRight)
        {
            return new WeeklyProgressionDecision(
                "hold", "reps", request.CurrentVolumeSetsDelta, 0, 0,
                request.ConsecutiveEasyWeeks, 0, false, BuildAttributeBias(request.Attributes));
        }

        // too_easy -> progredir em exatamente 1 eixo (RN-003), avançando o ciclo a partir do último eixo usado.
        var maxVolume = WeeklyProgressionState.MaxVolumeSetsDelta + (HasHighAttribute(request.Attributes, "vitality") ? 1 : 0);
        var axis = NextAxis(request.LastAxis, request.CurrentVolumeSetsDelta, maxVolume);

        var volumeDelta = axis == "add_set"
            ? Math.Min(maxVolume, request.CurrentVolumeSetsDelta + 1)
            : request.CurrentVolumeSetsDelta;
        var rpeDelta = axis == "higher_rpe" ? 1 : 0;
        var restDelta = axis == "less_rest" ? -10 : 0;

        return new WeeklyProgressionDecision(
            "progress", axis, volumeDelta, rpeDelta, restDelta,
            request.ConsecutiveEasyWeeks + 1, 0, false, BuildAttributeBias(request.Attributes));
    }

    private static string? DominantFeeling(IReadOnlyList<string> recentFeelings)
    {
        if (recentFeelings.Count == 0) return null;

        var priority = PerceivedFeelings.All.ToArray();
        return recentFeelings
            .GroupBy(f => f, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => Array.IndexOf(priority, g.Key)) // desempate determinístico
            .First().Key;
    }

    private static string NextAxis(string? lastAxis, int currentVolumeSetsDelta, int maxVolume)
    {
        var startIndex = lastAxis is null ? -1 : Array.IndexOf(AxisCycle, lastAxis);
        for (var offset = 1; offset <= AxisCycle.Length; offset++)
        {
            var candidate = AxisCycle[(startIndex + offset) % AxisCycle.Length];
            // RN-004: não insiste em "add_set" além do teto recuperável -> pula para o próximo eixo.
            if (candidate == "add_set" && currentVolumeSetsDelta >= maxVolume) continue;
            return candidate;
        }
        return AxisCycle[0];
    }

    private static bool HasHighAttribute(IReadOnlyDictionary<string, int> attributes, string attribute) =>
        attributes.TryGetValue(attribute, out var value) && value >= HighAttributeThreshold;

    /// US-241 §6.6: vetor de viés por atributo, dentro dos limites de segurança/recuperação
    /// (aplicado pelo chamador sobre a prescrição — este método só sinaliza a direção).
    private static IReadOnlyDictionary<string, int> BuildAttributeBias(IReadOnlyDictionary<string, int> attributes)
    {
        var bias = new Dictionary<string, int>(StringComparer.Ordinal);
        if (HasHighAttribute(attributes, "strength")) bias["strength"] = 1;   // reps- / variante mais pesada
        if (HasHighAttribute(attributes, "endurance")) bias["endurance"] = 1; // reps+ / descanso-
        if (HasHighAttribute(attributes, "vitality")) bias["vitality"] = 1;   // +1 teto de volume (já aplicado acima)
        return bias;
    }
}

public sealed record WeeklyProgressionDecisionRequest(
    IReadOnlyList<string> RecentFeelings,
    int ConsecutiveEasyWeeks,
    int ConsecutiveHardWeeks,
    int MesocycleWeekIndex,
    int CurrentVolumeSetsDelta,
    string Rank,
    IReadOnlyDictionary<string, int> Attributes,
    string? LastAxis = null);

public sealed record WeeklyProgressionDecision(
    string Decision,
    string? Axis,
    int VolumeSetsDelta,
    int RpeDelta,
    int RestSecondsDelta,
    int ConsecutiveEasyWeeks,
    int ConsecutiveHardWeeks,
    bool DeloadWeek,
    IReadOnlyDictionary<string, int> AttributeBias);
