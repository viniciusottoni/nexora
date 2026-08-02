namespace Awaken.Domain.Entities.Quests;

/// US-241 §6.2: proxy de 3 bandas para "RPE médio × RPE-alvo" — capturado como
/// uma pergunta simples ao usuário na conclusão da quest (decisão de escopo:
/// reps/sets sempre consideradas "atingidas conforme prescrito").
public static class PerceivedFeelings
{
    public const string TooEasy = "too_easy";
    public const string JustRight = "just_right";
    public const string TooHard = "too_hard";

    public static readonly IReadOnlyCollection<string> All = [TooEasy, JustRight, TooHard];

    public static bool IsValid(string? value) => value is null || All.Contains(value, StringComparer.Ordinal);
}
