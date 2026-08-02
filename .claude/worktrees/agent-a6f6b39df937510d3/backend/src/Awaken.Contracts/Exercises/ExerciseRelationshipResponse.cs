namespace Awaken.Contracts.Exercises;

/// <summary>
/// US-236 — candidato de relação (similar/substituição/progressão/regressão) pronto para consumo
/// externo. Nunca inclui <c>reasons</c> cru do dataset (RN-008) — o motivo estruturado, quando exibido
/// ao usuário final, deve ser resolvido por chave i18n a partir de <see cref="RelationCategory"/>/<see cref="Types"/>.
/// </summary>
public record ExerciseRelationshipResponse(
    string ExerciseId,
    string Name,
    string RelationCategory,
    IReadOnlyList<string> Types,
    decimal Score,
    string Confidence);
