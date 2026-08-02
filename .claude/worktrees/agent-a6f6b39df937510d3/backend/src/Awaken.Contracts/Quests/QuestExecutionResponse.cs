namespace Awaken.Contracts.Quests;

public record QuestExecutionResponse(
    Guid QuestId,
    string QuestType,
    string Status,
    DateTime StartedAtUtc,
    QuestAttributeXpDto AttributeXpPreview,
    IReadOnlyList<QuestExerciseDto> Exercises);

public record QuestExerciseDto(
    Guid QuestExerciseId,
    int Order,
    string Status,
    string Name,
    int Sets,
    int RepsMin,
    int? RepsMax,
    int? RestSeconds,
    string? TargetRpe,
    string? VideoUrl,
    long XpReward,
    int EffectiveDifficulty,
    QuestVisibleAttributeImpactsDto AttributeImpacts,
    QuestHiddenAttributeImpactsDto HiddenAttributeImpacts,
    DateTime? CompletedAtUtc,
    // US-236: id do exercicio no ExerciseCatalog, usado para consultar candidatos de
    // relacao (similar/substituicao/progressao/regressao) via GET /api/exercises/{id}/relationships.
    string? ProviderExerciseId = null,
    // US-041: instrucoes passo-a-passo e dicas, resolvidas do ExerciseCatalog via
    // ProviderExerciseId (QuestExercise nao guarda esse dado, so o id de referencia).
    IReadOnlyList<string>? Instructions = null,
    IReadOnlyList<string>? Tips = null);

public record QuestAttributeXpDto(
    int Strength,
    int Agility,
    int Endurance,
    int Vitality,
    int Focus,
    int Wisdom);

public record QuestVisibleAttributeImpactsDto(
    int Strength,
    int Agility,
    int Endurance,
    int Vitality,
    int Focus);

public record QuestHiddenAttributeImpactsDto(int Wisdom);
