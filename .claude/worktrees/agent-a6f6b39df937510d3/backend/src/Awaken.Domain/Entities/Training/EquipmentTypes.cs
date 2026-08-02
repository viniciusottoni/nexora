namespace Awaken.Domain.Entities.Training;

/// US-144/US-148 (R3.2): enum interno (string) de tipos de equipamento, usado para validar
/// <c>ExerciseCatalog.RequiredEquipment</c> na sanitização (RN-005 do US-148). Mesmo padrão de
/// <see cref="MuscleGroups"/>/<see cref="MovementPatterns"/> — fonte de verdade estável das chaves.
public static class EquipmentTypes
{
    public const string Barbell = "barbell";
    public const string Dumbbell = "dumbbell";
    public const string Cable = "cable";
    public const string Machine = "machine";
    public const string Bodyweight = "bodyweight";
    public const string Kettlebell = "kettlebell";
    public const string ResistanceBand = "resistance_band";
    public const string MedicineBall = "medicine_ball";
    public const string StabilityBall = "stability_ball";
    public const string EzBarbell = "ez_barbell";
    public const string SmithMachine = "smith_machine";
    public const string LeverageMachine = "leverage_machine";
    public const string Assisted = "assisted";
    public const string Rope = "rope";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Barbell, Dumbbell, Cable, Machine, Bodyweight, Kettlebell, ResistanceBand,
        MedicineBall, StabilityBall, EzBarbell, SmithMachine, LeverageMachine, Assisted, Rope,
    };

    public static bool IsValid(string value) => All.Contains(value);
}
