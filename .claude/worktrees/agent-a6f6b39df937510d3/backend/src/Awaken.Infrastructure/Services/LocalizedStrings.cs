namespace Awaken.Infrastructure.Services;

/// ADR-021: workout generation textos hardcoded devem respeitar User.PreferredLanguage,
/// nao usar inglês como default fixo independente do idioma do usuário.
internal static class LocalizedStrings
{
    public static string CatalogDescription(string language) => language switch
    {
        "en" => "Workout generated from the approved exercise catalog.",
        "es" => "Entrenamiento generado a partir del catálogo de ejercicios aprobado.",
        "fr" => "Entraînement généré à partir du catalogue d'exercices approuvé.",
        _ => "Treino gerado a partir do catálogo de exercícios aprovado.",
    };

    public static string FallbackDescription(string language) => language switch
    {
        "en" => "Full body workout",
        "es" => "Entrenamiento de cuerpo completo",
        "fr" => "Entraînement complet du corps",
        _ => "Treino de corpo inteiro",
    };

    public static string[] FallbackExerciseNames(string language) => language switch
    {
        "en" => ["Squat", "Push-up", "Plank"],
        "es" => ["Sentadilla", "Flexión de brazos", "Plancha"],
        "fr" => ["Squat", "Pompe", "Planche"],
        _ => ["Agachamento", "Flexão de braço", "Prancha"],
    };
}
