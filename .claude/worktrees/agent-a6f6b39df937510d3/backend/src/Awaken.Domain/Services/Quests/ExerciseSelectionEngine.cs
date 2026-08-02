using Awaken.Domain.Entities.Exercises;

namespace Awaken.Domain.Services.Quests;

/// <summary>
/// US-151: selects exercises within the time budget while keeping movement and
/// muscle-group balance.
/// </summary>
public static class ExerciseSelectionEngine
{
    private const double VarietyPatternBonus = 0.02;
    private const double VarietyMuscleBonus = 0.02;
    private const double Epsilon = 1e-9;
    // Low-priority criteria can flip only close scores, so balance can win ties.
    private const double TieBreakTolerance = 0.10;

    public static IReadOnlyList<ExerciseScore> Select(
        IReadOnlyList<ExerciseScore> scored,
        int availableMinutesPerWorkout)
    {
        var selected = new List<ExerciseScore>();
        var remaining = new List<ExerciseScore>(scored);
        var selectedPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selectedMuscleGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedMinutes = 0;

        while (remaining.Count > 0)
        {
            var bestIndex = -1;
            var bestScore = double.NegativeInfinity;
            var bestSelectionBonus = double.NegativeInfinity;
            ExerciseScore? best = null;

            for (var i = 0; i < remaining.Count; i++)
            {
                var candidate = remaining[i];
                var estimated = ExerciseScoringEngine.EstimatedDurationMinutes(candidate.Exercise);
                if (usedMinutes + estimated > availableMinutesPerWorkout) continue;

                var selectionBonus = ComputeVarietyBonus(candidate.Exercise, selectedPatterns, selectedMuscleGroups);
                var selectionScore = candidate.StaticScore + selectionBonus;

                if (selectionScore > bestScore + Epsilon ||
                    (selectionScore >= bestScore - TieBreakTolerance &&
                     IsPreferredTieBreak(candidate, best, selectionBonus, bestSelectionBonus)))
                {
                    bestScore = selectionScore;
                    bestSelectionBonus = selectionBonus;
                    bestIndex = i;
                    best = candidate;
                }
            }

            // No exercise fits the remaining time budget.
            if (bestIndex < 0) break;

            var chosen = remaining[bestIndex];
            remaining.RemoveAt(bestIndex);
            selected.Add(chosen);

            usedMinutes += ExerciseScoringEngine.EstimatedDurationMinutes(chosen.Exercise);
            selectedPatterns.Add(chosen.Exercise.MovementPattern);
            foreach (var muscle in chosen.Exercise.PrimaryMuscleGroups)
                selectedMuscleGroups.Add(muscle);
        }

        return selected;
    }

    private static bool IsPreferredTieBreak(
        ExerciseScore candidate,
        ExerciseScore? currentBest,
        double candidateSelectionBonus,
        double bestSelectionBonus)
    {
        if (currentBest is null) return true;

        if (candidateSelectionBonus > bestSelectionBonus + Epsilon) return true;
        if (candidateSelectionBonus + Epsilon < bestSelectionBonus) return false;

        if (candidate.VarietyScore > currentBest.VarietyScore + Epsilon) return true;
        if (candidate.VarietyScore + Epsilon < currentBest.VarietyScore) return false;

        if (candidate.SafetyScore > currentBest.SafetyScore + Epsilon) return true;
        if (candidate.SafetyScore + Epsilon < currentBest.SafetyScore) return false;

        return candidate.TargetAttributeScore > currentBest.TargetAttributeScore;
    }

    private static double ComputeVarietyBonus(
        ExerciseCatalog exercise,
        HashSet<string> selectedPatterns,
        HashSet<string> selectedMuscleGroups)
    {
        var patternBonus = selectedPatterns.Count == 0 ||
                           !selectedPatterns.Contains(exercise.MovementPattern)
            ? VarietyPatternBonus
            : 0.0;

        var hasMuscleOverlap = exercise.PrimaryMuscleGroups
            .Any(m => selectedMuscleGroups.Contains(m, StringComparer.OrdinalIgnoreCase));
        var muscleBonus = selectedMuscleGroups.Count == 0 || !hasMuscleOverlap
            ? VarietyMuscleBonus
            : 0.0;

        return patternBonus + muscleBonus;
    }
}
