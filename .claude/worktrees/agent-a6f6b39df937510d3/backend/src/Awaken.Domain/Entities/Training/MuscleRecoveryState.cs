using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Training;

/// US-239: estado de recuperação de um grupo muscular de um usuário,
/// atualizado apenas por treinos concluídos (RN-008 — quests não concluídas
/// não geram fadiga). Consumido por <see cref="Awaken.Domain.Services.Training.RecoveryPlanner"/>
/// para derivar status/cap de volume/RPE do dia.
public class MuscleRecoveryState : BaseEntity
{
    public Guid UserId { get; private set; }
    public string MuscleGroup { get; private set; } = string.Empty;
    public DateTime LastTrainedAtUtc { get; private set; }
    public string LastIntensity { get; private set; } = "moderate"; // light|moderate|heavy
    public List<string> LastMovementFamilies { get; private set; } = [];
    public int WeeklySetsAccumulated { get; private set; }
    public DateTime WeekAnchorDateUtc { get; private set; }

    private MuscleRecoveryState() { }

    public static MuscleRecoveryState CreateInitial(Guid userId, string muscleGroup, DateTime utcNow) => new()
    {
        UserId = userId,
        MuscleGroup = muscleGroup,
        LastTrainedAtUtc = utcNow,
        WeekAnchorDateUtc = StartOfWeek(utcNow),
    };

    /// RN-008: só treinos concluídos chamam isto. Acumula séries semanais
    /// dentro da mesma semana (âncora em domingo) e reinicia o acumulado
    /// quando a sessão cai numa semana nova (RN-002/CA-004).
    public void RegisterSession(DateTime utcNow, string intensity, IReadOnlyCollection<string> movementFamilies, int setsPerformed)
    {
        if (StartOfWeek(utcNow) != WeekAnchorDateUtc)
        {
            WeeklySetsAccumulated = 0;
            WeekAnchorDateUtc = StartOfWeek(utcNow);
        }

        LastTrainedAtUtc = utcNow;
        LastIntensity = intensity;
        LastMovementFamilies = movementFamilies.ToList();
        WeeklySetsAccumulated += setsPerformed;
        UpdatedAtUtc = utcNow;
    }

    private static DateTime StartOfWeek(DateTime utc) =>
        utc.Date.AddDays(-(int)utc.DayOfWeek);
}
