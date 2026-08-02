namespace Awaken.Domain.Entities.Training;

/// US-145/US-236/US-237: enum interno (string) de padrões de movimento,
/// usado para validar o alvo de cada dia do split map (RN-005). Lista
/// canônica da seção 7 do README do EPIC-005.
public static class MovementPatterns
{
    public const string Squat = "squat";
    public const string Hinge = "hinge";
    public const string HorizontalPush = "horizontal_push";
    public const string VerticalPush = "vertical_push";
    public const string HorizontalPull = "horizontal_pull";
    public const string VerticalPull = "vertical_pull";
    public const string Lunge = "lunge";
    public const string Carry = "carry";
    public const string CoreFlexion = "core_flexion";
    public const string CoreAntiExtension = "core_anti_extension";
    public const string CoreAntiRotation = "core_anti_rotation";
    public const string Locomotion = "locomotion";
    public const string Jump = "jump";
    public const string Balance = "balance";
    public const string Mobility = "mobility";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Squat, Hinge, HorizontalPush, VerticalPush, HorizontalPull, VerticalPull,
        Lunge, Carry, CoreFlexion, CoreAntiExtension, CoreAntiRotation, Locomotion, Jump, Balance, Mobility,
    };

    public static bool IsValid(string value) => All.Contains(value);
}
