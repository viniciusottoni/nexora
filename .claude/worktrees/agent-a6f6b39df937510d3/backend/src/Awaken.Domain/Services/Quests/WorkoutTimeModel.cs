namespace Awaken.Domain.Services.Quests;

/// <summary>
/// US-242 §6.1 — constantes do modelo de tempo determinístico. Versionado via
/// deploy de código (mesmo precedente de <see cref="ExercisePrescriptionEngine"/>/
/// <see cref="Awaken.Domain.Services.Training.RecoveryPlanner"/> — nenhum modelo
/// determinístico do catálogo usa tabela de config).
/// </summary>
public static class WorkoutTimeModel
{
    public const string Version = "v1";
    public const int SecondsPerRep = 3;
    public const int TransitionSeconds = 45;
    public const int CooldownSeconds = 120;
    public const int MicroQuestWarmupSeconds = 120;
    public const int MicroQuestThresholdMinutes = 15;
    public const decimal MinUtilization = 0.85m;

    public static int WarmupSecondsFor(string effectiveExperienceLevel) =>
        effectiveExperienceLevel.ToLowerInvariant() switch
        {
            "sedentary" or "beginner" => 300,
            "intermediate" => 420,
            "advanced" => 540,
            _ => 300,
        };
}
