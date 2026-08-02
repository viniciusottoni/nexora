using Awaken.Domain.Common;
using Awaken.Domain.Entities.Training;

namespace Awaken.Domain.Entities.Exercises;

public class ExerciseCatalog : BaseEntity
{
    public Guid? RawImportId { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderExerciseId { get; private set; } = string.Empty;
    public string? ProviderVersion { get; private set; }
    public string NamePtBr { get; private set; } = string.Empty;
    public string NameOriginal { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? DescriptionPtBr { get; private set; }
    public List<string> InstructionsPtBr { get; private set; } = [];
    public List<string> InstructionsOriginal { get; private set; } = [];
    public List<string> TipsPtBr { get; private set; } = [];
    public string ExerciseType { get; private set; } = string.Empty;
    public string DifficultyLevel { get; private set; } = string.Empty;
    public int DifficultyRank { get; private set; }
    public int TechnicalComplexity { get; private set; }
    public int ImpactLevel { get; private set; }
    public string Environment { get; private set; } = string.Empty;
    public List<string> RequiredEquipment { get; private set; } = [];
    public List<string> PrimaryMuscleGroups { get; private set; } = [];
    public List<string> SecondaryMuscleGroups { get; private set; } = [];
    public List<string> BodyParts { get; private set; } = [];
    public List<string> JointStressTags { get; private set; } = [];
    public List<string> ContraindicationTags { get; private set; } = [];
    public List<string> LimitationBlockTags { get; private set; } = [];
    public List<string> PainBlockTags { get; private set; } = [];
    public List<string> GoalTags { get; private set; } = [];
    public List<string> RiskTags { get; private set; } = [];
    public List<string> AccessibilityTags { get; private set; } = [];
    public string MinExperienceLevel { get; private set; } = string.Empty;
    public bool SuitableForSedentary { get; private set; }
    public bool SuitableForBeginner { get; private set; }
    public bool SuitableForIntermediate { get; private set; }
    public bool SuitableForAdvanced { get; private set; }
    public List<string> RegressionExerciseIds { get; private set; } = [];
    public List<string> ProgressionExerciseIds { get; private set; } = [];
    public List<string> RelatedExerciseIds { get; private set; } = [];
    public string? VideoUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? GifUrl { get; private set; }
    public string? MediaLicenseInfo { get; private set; }
    public string SanitizationStatus { get; private set; } = "pending_review";
    public bool IsApprovedForWorkoutGeneration { get; private set; }
    public ExerciseAttributeContribution? AttributeContribution { get; private set; }
    public List<ExerciseRelationship> Relations { get; private set; } = [];

    /// <summary>
    /// US-148 (R3.2) — lista calculada de códigos de problema de sanitização (ex.: "equipment_unmapped",
    /// "difficulty_out_of_range", "missing_goal_tags", "missing_primary_muscle"). Nunca é input externo:
    /// é sempre recomputada a partir dos próprios campos do catálogo dentro de <see cref="Apply"/> — ver
    /// <see cref="RecomputeSanitizationIssues"/>. Bloqueia <see cref="CanBeApproved"/> quando não vazia (RN-009).
    /// </summary>
    public List<string> SanitizationIssues { get; private set; } = [];

    public string? RejectionReason { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }

    /// <summary>
    /// US-236 — taxonomia biomecânica extraída para tabela própria 1:1 (RN-003). Nunca setado
    /// diretamente fora de <see cref="Apply"/>; os getters abaixo delegam para cá e não têm coluna
    /// própria em <c>exercise_catalogs</c> (sem duplicação de armazenamento).
    /// </summary>
    public ExerciseTaxonomy? Taxonomy { get; private set; }

    public string MovementPattern => Taxonomy?.MovementPattern ?? string.Empty;
    public string MovementFamily => Taxonomy?.MovementFamily ?? string.Empty;
    public string Mechanic => Taxonomy?.Mechanic ?? string.Empty;
    public string ForceType => Taxonomy?.ForceType ?? string.Empty;
    public string PlaneOfMotion => Taxonomy?.PlaneOfMotion ?? string.Empty;
    public string Laterality => Taxonomy?.Laterality ?? string.Empty;
    public string BodyPosition => Taxonomy?.BodyPosition ?? string.Empty;
    public string? BenchAngle => Taxonomy?.BenchAngle;
    public string EquipmentCategory => Taxonomy?.EquipmentCategory ?? string.Empty;
    public string LoadType => Taxonomy?.LoadType ?? string.Empty;
    public string PrimaryRegion => Taxonomy?.PrimaryRegion ?? string.Empty;
    public bool IsCompound => Taxonomy?.IsCompound ?? false;
    public bool IsUnilateral => Taxonomy?.IsUnilateral ?? false;
    public bool IsAssisted => Taxonomy?.IsAssisted ?? false;
    public bool IsWeighted => Taxonomy?.IsWeighted ?? false;
    public List<string> TaxonomySignals => Taxonomy?.Signals ?? [];
    public string Confidence => Taxonomy?.Confidence ?? "medium";

    private ExerciseCatalog() { }

    public static ExerciseCatalog Create(ExerciseCatalogSnapshot snapshot, DateTime utcNow)
    {
        var exercise = new ExerciseCatalog();
        exercise.Apply(snapshot, utcNow);
        return exercise;
    }

    public void Apply(ExerciseCatalogSnapshot snapshot, DateTime utcNow)
    {
        RawImportId = snapshot.RawImportId;
        ProviderName = snapshot.ProviderName;
        ProviderExerciseId = snapshot.ProviderExerciseId;
        ProviderVersion = snapshot.ProviderVersion;
        NamePtBr = snapshot.NamePtBr;
        NameOriginal = snapshot.NameOriginal;
        Slug = snapshot.Slug;
        DescriptionPtBr = snapshot.DescriptionPtBr;
        InstructionsPtBr = snapshot.InstructionsPtBr;
        InstructionsOriginal = snapshot.InstructionsOriginal;
        TipsPtBr = snapshot.TipsPtBr;
        ExerciseType = snapshot.ExerciseType;
        DifficultyLevel = snapshot.DifficultyLevel;
        DifficultyRank = snapshot.DifficultyRank;
        TechnicalComplexity = snapshot.TechnicalComplexity;
        ImpactLevel = snapshot.ImpactLevel;
        Environment = snapshot.Environment;
        RequiredEquipment = snapshot.RequiredEquipment;
        PrimaryMuscleGroups = snapshot.PrimaryMuscleGroups;
        SecondaryMuscleGroups = snapshot.SecondaryMuscleGroups;
        BodyParts = snapshot.BodyParts;
        JointStressTags = snapshot.JointStressTags;
        ContraindicationTags = snapshot.ContraindicationTags;
        LimitationBlockTags = snapshot.LimitationBlockTags;
        PainBlockTags = snapshot.PainBlockTags;
        GoalTags = snapshot.GoalTags;
        RiskTags = snapshot.RiskTags;
        AccessibilityTags = snapshot.AccessibilityTags;
        MinExperienceLevel = snapshot.MinExperienceLevel;
        SuitableForSedentary = snapshot.SuitableForSedentary;
        SuitableForBeginner = snapshot.SuitableForBeginner;
        SuitableForIntermediate = snapshot.SuitableForIntermediate;
        SuitableForAdvanced = snapshot.SuitableForAdvanced;
        RegressionExerciseIds = snapshot.RegressionExerciseIds;
        ProgressionExerciseIds = snapshot.ProgressionExerciseIds;
        RelatedExerciseIds = snapshot.RelatedExerciseIds;
        VideoUrl = snapshot.VideoUrl;
        ImageUrl = snapshot.ImageUrl;
        GifUrl = snapshot.GifUrl;
        MediaLicenseInfo = snapshot.MediaLicenseInfo;
        SanitizationStatus = snapshot.SanitizationStatus;
        IsApprovedForWorkoutGeneration = snapshot.IsApprovedForWorkoutGeneration;

        var taxonomySnapshot = new ExerciseTaxonomySnapshot(
            snapshot.MovementFamily,
            snapshot.MovementPattern,
            snapshot.Mechanic,
            snapshot.ForceType,
            snapshot.PlaneOfMotion,
            snapshot.Laterality,
            snapshot.BodyPosition,
            snapshot.BenchAngle,
            snapshot.EquipmentCategory,
            snapshot.LoadType,
            snapshot.PrimaryRegion,
            snapshot.IsCompound,
            snapshot.IsUnilateral,
            snapshot.IsAssisted,
            snapshot.IsWeighted,
            snapshot.TaxonomySignals,
            snapshot.Confidence);

        if (Taxonomy is null)
            Taxonomy = ExerciseTaxonomy.Create(Id, taxonomySnapshot);
        else
            Taxonomy.Apply(taxonomySnapshot, utcNow);

        RecomputeSanitizationIssues();
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// US-148 (R3.2) — recalcula <see cref="SanitizationIssues"/> a partir dos campos já aplicados
    /// neste catálogo (RN-002/RN-005/RN-006/RN-007 do US-148). Não faz I/O, não lança exceção: só
    /// registra os problemas encontrados para os consumidores (<see cref="CanBeApproved"/>, curadoria).
    /// </summary>
    private void RecomputeSanitizationIssues()
    {
        var issues = new List<string>();

        // RN-005: equipamento deve estar mapeado e o tipo deve ser válido.
        if (RequiredEquipment.Count == 0 || RequiredEquipment.Any(equipment => !EquipmentTypes.IsValid(equipment)))
            issues.Add("equipment_unmapped");

        // RN-006: dificuldade deve estar na faixa 1-5.
        if (DifficultyRank is < 1 or > 5)
            issues.Add("difficulty_out_of_range");

        // RN-007: deve ter afinidade com pelo menos 1 objetivo.
        if (GoalTags.Count == 0)
            issues.Add("missing_goal_tags");

        // RN-002: deve existir pelo menos 1 grupo muscular principal.
        if (PrimaryMuscleGroups.Count == 0)
            issues.Add("missing_primary_muscle");

        SanitizationIssues = issues;
    }

    /// <summary>
    /// US-236: aplica somente a camada enriquecida sobre um catálogo previamente
    /// normalizado. A classificação-base continua sendo a autoridade e uma
    /// divergência de taxonomia fica pendente para revisão manual.
    /// </summary>
    public bool ApplyEnrichment(ExerciseCatalogSnapshot snapshot, DateTime utcNow)
    {
        RawImportId = snapshot.RawImportId;
        ProviderVersion = snapshot.ProviderVersion ?? ProviderVersion;
        GifUrl = snapshot.GifUrl ?? GifUrl;
        MediaLicenseInfo = snapshot.MediaLicenseInfo ?? MediaLicenseInfo;
        RegressionExerciseIds = snapshot.RegressionExerciseIds;
        ProgressionExerciseIds = snapshot.ProgressionExerciseIds;
        RelatedExerciseIds = snapshot.RelatedExerciseIds;

        var hasConflict = HasTaxonomyConflict(snapshot);
        if (!hasConflict)
        {
            var taxonomySnapshot = new ExerciseTaxonomySnapshot(
                snapshot.MovementFamily,
                snapshot.MovementPattern,
                snapshot.Mechanic,
                snapshot.ForceType,
                snapshot.PlaneOfMotion,
                snapshot.Laterality,
                snapshot.BodyPosition,
                snapshot.BenchAngle,
                snapshot.EquipmentCategory,
                snapshot.LoadType,
                snapshot.PrimaryRegion,
                snapshot.IsCompound,
                snapshot.IsUnilateral,
                snapshot.IsAssisted,
                snapshot.IsWeighted,
                snapshot.TaxonomySignals,
                snapshot.Confidence);

            if (Taxonomy is null)
                Taxonomy = ExerciseTaxonomy.Create(Id, taxonomySnapshot);
            else
                Taxonomy.Apply(taxonomySnapshot, utcNow);
        }
        else
        {
            SanitizationStatus = "pending_review";
            IsApprovedForWorkoutGeneration = false;
        }

        UpdatedAtUtc = utcNow;
        return !hasConflict;
    }

    public void MarkPendingReview(DateTime utcNow)
    {
        SanitizationStatus = "pending_review";
        IsApprovedForWorkoutGeneration = false;
        UpdatedAtUtc = utcNow;
    }

    private bool HasTaxonomyConflict(ExerciseCatalogSnapshot snapshot)
    {
        if (Taxonomy is null)
            return false;

        return Conflicts(MovementPattern, snapshot.MovementPattern)
            || Conflicts(MovementFamily, snapshot.MovementFamily)
            || Conflicts(EquipmentCategory, snapshot.EquipmentCategory)
            || Conflicts(PrimaryRegion, snapshot.PrimaryRegion);
    }

    private static bool Conflicts(string current, string imported) =>
        !string.IsNullOrWhiteSpace(current)
        && !string.IsNullOrWhiteSpace(imported)
        && !string.Equals(current, imported, StringComparison.OrdinalIgnoreCase);

    public bool CanBeApproved() =>
        InstructionsPtBr.Count > 0
        && PrimaryMuscleGroups.Count > 0
        && AttributeContribution is not null
        && AttributeContribution.IsValidForApprovedExercise()
        && !string.IsNullOrWhiteSpace(GifUrl) // RN-006 (US-236): GIF 360 obrigatorio para aprovacao.
        && SanitizationIssues.Count == 0; // RN-009 (US-148): nenhuma pendencia de sanitizacao.

    public void SetAttributeContribution(ExerciseAttributeContribution contribution, DateTime utcNow)
    {
        AttributeContribution = contribution;
        UpdatedAtUtc = utcNow;
    }

    public void ReplaceRelations(IEnumerable<ExerciseRelationship> relations, DateTime utcNow)
    {
        Relations.Clear();
        Relations.AddRange(relations);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// US-149 (R3.3) — <paramref name="reviewedBy"/> é opcional para preservar a aprovação automática
    /// do import (RN de US-236/US-143, sem curador humano envolvido). Quando um curador aprova
    /// manualmente um item <c>pending_review</c> (via <c>ApproveExerciseCommand</c>), passa o próprio
    /// identificador para deixar a trilha de auditoria (RN-006 do US-149).
    /// </summary>
    public void ApproveForWorkoutGeneration(DateTime utcNow, string? reviewedBy = null)
    {
        if (!CanBeApproved())
            throw new InvalidOperationException(
                "Exercise cannot be approved without media, instructions, primary muscle and valid attributes."
            );

        IsApprovedForWorkoutGeneration = true;
        SanitizationStatus = "approved";
        RejectionReason = null;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// US-149 (R3.3) — RN-005: exercício reprovado recebe <c>rejected</c> com motivo. Pode reverter
    /// um exercício previamente <c>approved</c> (curadoria pode revisitar decisões antigas).
    /// </summary>
    public void Reject(string reason, string? reviewedBy, DateTime utcNow)
    {
        SanitizationStatus = "rejected";
        IsApprovedForWorkoutGeneration = false;
        RejectionReason = reason;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}

public record ExerciseCatalogSnapshot(
    Guid? RawImportId,
    string ProviderName,
    string ProviderExerciseId,
    string? ProviderVersion,
    string NamePtBr,
    string NameOriginal,
    string Slug,
    string? DescriptionPtBr,
    List<string> InstructionsPtBr,
    List<string> InstructionsOriginal,
    List<string> TipsPtBr,
    string ExerciseType,
    string MovementPattern,
    string MovementFamily,
    string Mechanic,
    string ForceType,
    string PlaneOfMotion,
    string Laterality,
    string BodyPosition,
    string? BenchAngle,
    string EquipmentCategory,
    string LoadType,
    string PrimaryRegion,
    string DifficultyLevel,
    int DifficultyRank,
    int TechnicalComplexity,
    int ImpactLevel,
    string Environment,
    List<string> RequiredEquipment,
    List<string> PrimaryMuscleGroups,
    List<string> SecondaryMuscleGroups,
    List<string> BodyParts,
    List<string> JointStressTags,
    List<string> ContraindicationTags,
    List<string> LimitationBlockTags,
    List<string> PainBlockTags,
    List<string> GoalTags,
    List<string> RiskTags,
    List<string> AccessibilityTags,
    List<string> TaxonomySignals,
    string MinExperienceLevel,
    bool SuitableForSedentary,
    bool SuitableForBeginner,
    bool SuitableForIntermediate,
    bool SuitableForAdvanced,
    bool IsCompound,
    bool IsUnilateral,
    bool IsAssisted,
    bool IsWeighted,
    List<string> RegressionExerciseIds,
    List<string> ProgressionExerciseIds,
    List<string> RelatedExerciseIds,
    string? VideoUrl,
    string? ImageUrl,
    string? GifUrl,
    string? MediaLicenseInfo,
    string SanitizationStatus,
    bool IsApprovedForWorkoutGeneration,
    string Confidence);
