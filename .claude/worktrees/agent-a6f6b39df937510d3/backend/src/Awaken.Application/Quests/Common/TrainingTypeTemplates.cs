using System.Text.Json;

namespace Awaken.Application.Quests.Common;

/// US-051: templates de treino para tipos que não usam geração personalizada
/// (regeneração e programas fixos). Estrutura compatível com QuestResponseMapper.
public static class TrainingTypeTemplates
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string RegenerationWorkoutJson(string language) =>
        JsonSerializer.Serialize(new
        {
            title = language == "en" ? "Recovery Training"
                  : language == "es" ? "Entrenamiento de Recuperación"
                  : "Treino de Regeneração",
            description = language == "en" ? "Light mobility and recovery session."
                        : language == "es" ? "Sesión ligera de movilidad y recuperación."
                        : "Sessão leve de mobilidade e recuperação.",
            durationMinutes = 20,
            exercises = new[]
            {
                new { name = language == "en" ? "Cat-Cow" : language == "es" ? "Gato-Vaca" : "Gato-Vaca",
                      description = "Mobilidade de coluna", sets = 2, repsMin = 10, repsMax = 12, restSeconds = 30, targetRpe = "2-3" },
                new { name = language == "en" ? "Hip Flexor Stretch" : "Alongamento de Flexor do Quadril",
                      description = "Flexibilidade de quadril", sets = 2, repsMin = 30, repsMax = 30, restSeconds = 20, targetRpe = "1-2" },
                new { name = language == "en" ? "Shoulder Circles" : language == "es" ? "Círculos de Hombros" : "Circundução de Ombros",
                      description = "Mobilidade de ombro", sets = 2, repsMin = 10, repsMax = 10, restSeconds = 20, targetRpe = "1-2" },
                new { name = language == "en" ? "Child's Pose" : language == "es" ? "Postura del Niño" : "Posição do Bebê",
                      description = "Relaxamento lombar e quadril", sets = 2, repsMin = 30, repsMax = 30, restSeconds = 20, targetRpe = "1" },
            }
        }, Json);

    public static string SaitamaPathWorkoutJson(string language) =>
        JsonSerializer.Serialize(new
        {
            title = language == "en" ? "Saitama Path"
                  : language == "es" ? "Camino de Saitama"
                  : "Caminho de Saitama",
            description = language == "en"
                ? "100 push-ups, 100 sit-ups, 100 squats, 10 km run — every day."
                : language == "es"
                ? "100 flexiones, 100 abdominales, 100 sentadillas, 10 km de carrera — todos los días."
                : "100 flexões, 100 abdominais, 100 agachamentos, 10 km de corrida — todo dia.",
            durationMinutes = 60,
            exercises = new[]
            {
                new { name = language == "en" ? "Push-up" : "Flexão", description = "Peito e tríceps", sets = 10, repsMin = 10, repsMax = 10, restSeconds = 60, targetRpe = "7-9" },
                new { name = language == "en" ? "Sit-up" : "Abdominal", description = "Core", sets = 10, repsMin = 10, repsMax = 10, restSeconds = 60, targetRpe = "7-9" },
                new { name = language == "en" ? "Squat" : language == "es" ? "Sentadilla" : "Agachamento", description = "Pernas e glúteos", sets = 10, repsMin = 10, repsMax = 10, restSeconds = 60, targetRpe = "7-9" },
            }
        }, Json);

    public static string Perfect2WorkoutJson(string language) =>
        JsonSerializer.Serialize(new
        {
            title = language == "en" ? "Perfect 2"
                  : language == "es" ? "Perfecto 2"
                  : "Perfect 2",
            description = language == "en"
                ? "Hybrid strength and hypertrophy program. 2 days on, 1 day off."
                : language == "es"
                ? "Programa híbrido de fuerza e hipertrofia. 2 días de entrenamiento, 1 de descanso."
                : "Programa híbrido de força e hipertrofia. 2 dias de treino, 1 de descanso.",
            durationMinutes = 45,
            exercises = new[]
            {
                new { name = language == "en" ? "Barbell Squat" : language == "es" ? "Sentadilla con Barra" : "Agachamento com Barra", description = "Pernas", sets = 4, repsMin = 6, repsMax = 8, restSeconds = 120, targetRpe = "7-9" },
                new { name = language == "en" ? "Bench Press" : language == "es" ? "Press de Banca" : "Supino", description = "Peito", sets = 4, repsMin = 6, repsMax = 8, restSeconds = 120, targetRpe = "7-9" },
                new { name = language == "en" ? "Pull-up" : "Puxada", description = "Costas e bíceps", sets = 3, repsMin = 8, repsMax = 12, restSeconds = 90, targetRpe = "7-8" },
                new { name = language == "en" ? "Romanian Deadlift" : language == "es" ? "Peso Muerto Rumano" : "Levantamento Terra Romeno", description = "Posterior de coxa", sets = 3, repsMin = 8, repsMax = 10, restSeconds = 120, targetRpe = "7-9" },
            }
        }, Json);
}
