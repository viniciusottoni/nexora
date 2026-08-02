namespace Awaken.Domain.Entities.Training;

/// US-036/US-237: enum interno (string) de grupos musculares, usado para
/// validar o alvo de cada dia do split map (RN-005). Ainda não existe um
/// enum formal de grupo muscular no catálogo — esta classe é a fonte de
/// verdade estável dessas chaves, no mesmo padrão de <see cref="TrainingProgramKeys"/>.
public static class MuscleGroups
{
    public const string Chest = "chest";
    public const string Back = "back";
    public const string Shoulders = "shoulders";
    public const string Biceps = "biceps";
    public const string Triceps = "triceps";
    public const string Forearms = "forearms";
    public const string Traps = "traps";
    public const string RearDelts = "rear_delts";
    public const string Quadriceps = "quadriceps";
    public const string Hamstrings = "hamstrings";
    public const string Glutes = "glutes";
    public const string Calves = "calves";
    public const string Core = "core";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Chest, Back, Shoulders, Biceps, Triceps, Forearms, Traps, RearDelts,
        Quadriceps, Hamstrings, Glutes, Calves, Core,
    };

    public static bool IsValid(string value) => All.Contains(value);
}
