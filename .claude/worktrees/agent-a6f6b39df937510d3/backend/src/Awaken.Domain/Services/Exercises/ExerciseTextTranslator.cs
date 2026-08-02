namespace Awaken.Domain.Services.Exercises;

/// <summary>
/// US-144 (R3.1) — tradutor determinístico por substituição de palavras/frases, sem chamada de API
/// externa (mantém consistência com o resto do catálogo, que é 100% determinístico e sem IA em runtime).
/// O dicionário é deliberadamente pequeno e incompleto: o objetivo não é tradução perfeita (isso exigiria
/// um serviço de tradução real, fora de escopo), é sair do estado atual (zero tradução, cópia literal do
/// inglês) para um estado testável e auditável (tradução parcial determinística + marcação clara de quando
/// a tradução não está completa via <see cref="ExerciseTranslationResult.IsFullyTranslated"/>).
/// </summary>
public static class ExerciseTextTranslator
{
    // Dicionario curado, ordenado por tamanho de frase decrescente (match de frase antes de palavra solta).
    private static readonly (string En, string Pt)[] PhraseDictionary =
    [
        ("assisted", "assistido"), ("barbell", "barra"), ("dumbbell", "halter"),
        ("bench press", "supino"), ("push-up", "flexao"), ("push up", "flexao"),
        ("pull-up", "barra fixa"), ("pull up", "barra fixa"), ("squat", "agachamento"),
        ("lunge", "avanco"), ("curl", "rosca"), ("row", "remada"), ("raise", "elevacao"),
        ("extension", "extensao"), ("flexion", "flexao"), ("seated", "sentado"),
        ("standing", "em pe"), ("incline", "inclinado"), ("decline", "declinado"),
        ("lying", "deitado"), ("kneeling", "ajoelhado"), ("cable", "cabo"),
        ("machine", "maquina"), ("smith", "smith"), ("kettlebell", "kettlebell"),
        ("band", "elastico"), ("chest", "peito"), ("back", "costas"), ("shoulder", "ombro"),
        ("bicep", "biceps"), ("tricep", "triceps"), ("leg", "perna"), ("calf", "panturrilha"),
        ("glute", "gluteo"), ("hip", "quadril"), ("knee", "joelho"), ("wide", "aberto"),
        ("narrow", "fechado"), ("close", "fechado"), ("grip", "pegada"), ("reverse", "reverso"),
        ("single", "unilateral"), ("alternating", "alternado"), ("with", "com"), ("and", "e"), ("the", ""),
    ];

    public static ExerciseTranslationResult Translate(string englishText)
    {
        if (string.IsNullOrWhiteSpace(englishText))
            return new ExerciseTranslationResult(englishText, true);

        var remaining = englishText.ToLowerInvariant();
        var untranslatedWords = new List<string>();

        foreach (var (en, pt) in PhraseDictionary)
            remaining = System.Text.RegularExpressions.Regex.Replace(
                remaining, $@"\b{System.Text.RegularExpressions.Regex.Escape(en)}\b", pt,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var translatedWords = PhraseDictionary
            .SelectMany(pair => System.Text.RegularExpressions.Regex
                .Matches(pair.Pt, "[a-z]+")
                .Cast<System.Text.RegularExpressions.Match>())
            .Select(match => match.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var match in System.Text.RegularExpressions.Regex.Matches(remaining, "[a-z]+")
                     .Cast<System.Text.RegularExpressions.Match>())
        {
            var word = match.Value;
            if (word.Length >= 3 && !translatedWords.Contains(word))
                untranslatedWords.Add(word);
        }

        var capitalized = CapitalizeFirst(remaining.Trim());
        return new ExerciseTranslationResult(capitalized, untranslatedWords.Count == 0);
    }

    private static string CapitalizeFirst(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}

public record ExerciseTranslationResult(string TranslatedText, bool IsFullyTranslated);
