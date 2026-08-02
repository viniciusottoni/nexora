using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Exercises;

/// <summary>
/// US-236 — candidato de relação (similar, substituição, progressão ou regressão) do exercício,
/// com score/confiança/motivos ranqueáveis (RN-004). Renomeada de <c>ExerciseCatalogRelation</c>
/// (rename de tabela via migration, não tabela paralela — o dado já existente é preservado).
/// </summary>
public class ExerciseRelationship : BaseEntity
{
    public Guid ExerciseCatalogId { get; private set; }
    public Guid? TargetExerciseCatalogId { get; private set; }
    public string RelatedProviderExerciseId { get; private set; } = string.Empty;
    public string RelatedName { get; private set; } = string.Empty;
    public string RelationKind { get; private set; } = string.Empty;
    public List<string> Types { get; private set; } = [];
    public decimal Score { get; private set; }
    public string Confidence { get; private set; } = "medium";
    public List<string> Reasons { get; private set; } = [];
    public string? DatasetVersion { get; private set; }

    private ExerciseRelationship() { }

    public static ExerciseRelationship Create(
        string relatedProviderExerciseId,
        string relatedName,
        string relationKind,
        IEnumerable<string> types,
        decimal score,
        string confidence,
        IEnumerable<string> reasons,
        string? datasetVersion = null,
        Guid? targetExerciseCatalogId = null)
    {
        return new ExerciseRelationship
        {
            TargetExerciseCatalogId = targetExerciseCatalogId,
            RelatedProviderExerciseId = relatedProviderExerciseId,
            RelatedName = relatedName,
            RelationKind = relationKind,
            Types = types.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Score = score,
            Confidence = confidence,
            Reasons = reasons.ToList(),
            DatasetVersion = datasetVersion,
        };
    }
}
