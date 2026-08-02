using Awaken.Domain.Entities.Exercises;
using FluentAssertions;

namespace Awaken.UnitTests.Exercises;

/// <summary>
/// US-236 — testes de <see cref="ExerciseCatalog"/> cobrindo a extração de <see cref="ExerciseTaxonomy"/>
/// (tabela nova 1:1, sem duplicar dado — ver getters delegados) e o aperto de <see cref="ExerciseCatalog.CanBeApproved"/>
/// para exigir GIF 360 (RN-006).
/// </summary>
public class ExerciseCatalogTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);

    private static ExerciseCatalogSnapshot BuildSnapshot(
        string? videoUrl = null,
        string? imageUrl = null,
        string? gifUrl = null,
        string movementPattern = "squat",
        List<string>? requiredEquipment = null,
        int difficultyRank = 3,
        List<string>? goalTags = null,
        List<string>? primaryMuscleGroups = null) =>
        new(
            RawImportId: null,
            ProviderName: "local_files",
            ProviderExerciseId: "0001",
            ProviderVersion: null,
            NamePtBr: "Agachamento",
            NameOriginal: "Squat",
            Slug: "agachamento",
            DescriptionPtBr: "Descricao",
            InstructionsPtBr: ["Passo 1"],
            InstructionsOriginal: ["Step 1"],
            TipsPtBr: [],
            ExerciseType: "strength",
            MovementPattern: movementPattern,
            MovementFamily: "squat_family",
            Mechanic: "compound",
            ForceType: "push",
            PlaneOfMotion: "sagittal",
            Laterality: "bilateral",
            BodyPosition: "standing",
            BenchAngle: null,
            EquipmentCategory: "free_weight",
            LoadType: "free_weight",
            PrimaryRegion: "lower_body",
            DifficultyLevel: "intermediate",
            DifficultyRank: difficultyRank,
            TechnicalComplexity: 3,
            ImpactLevel: 2,
            Environment: "gym",
            RequiredEquipment: requiredEquipment ?? ["barbell"],
            PrimaryMuscleGroups: primaryMuscleGroups ?? ["quadriceps"],
            SecondaryMuscleGroups: ["glutes"],
            BodyParts: ["legs"],
            JointStressTags: [],
            ContraindicationTags: [],
            LimitationBlockTags: [],
            PainBlockTags: [],
            GoalTags: goalTags ?? ["maintenance"],
            RiskTags: [],
            AccessibilityTags: [],
            TaxonomySignals: ["external_load"],
            MinExperienceLevel: "intermediate",
            SuitableForSedentary: false,
            SuitableForBeginner: false,
            SuitableForIntermediate: true,
            SuitableForAdvanced: true,
            IsCompound: true,
            IsUnilateral: false,
            IsAssisted: false,
            IsWeighted: true,
            RegressionExerciseIds: [],
            ProgressionExerciseIds: [],
            RelatedExerciseIds: [],
            VideoUrl: videoUrl,
            ImageUrl: imageUrl,
            GifUrl: gifUrl,
            MediaLicenseInfo: null,
            SanitizationStatus: "pending_review",
            IsApprovedForWorkoutGeneration: false,
            Confidence: "high");

    [Fact]
    public void CreatePopulatesTaxonomyChildAndDelegatedGettersReadFromIt()
    {
        var snapshot = BuildSnapshot();

        var catalog = ExerciseCatalog.Create(snapshot, UtcNow);

        catalog.Taxonomy.Should().NotBeNull();
        catalog.Taxonomy!.ExerciseCatalogId.Should().Be(catalog.Id);
        catalog.MovementPattern.Should().Be("squat");
        catalog.MovementFamily.Should().Be("squat_family");
        catalog.Mechanic.Should().Be("compound");
        catalog.ForceType.Should().Be("push");
        catalog.PlaneOfMotion.Should().Be("sagittal");
        catalog.Laterality.Should().Be("bilateral");
        catalog.BodyPosition.Should().Be("standing");
        catalog.EquipmentCategory.Should().Be("free_weight");
        catalog.LoadType.Should().Be("free_weight");
        catalog.PrimaryRegion.Should().Be("lower_body");
        catalog.IsCompound.Should().BeTrue();
        catalog.IsWeighted.Should().BeTrue();
        catalog.TaxonomySignals.Should().Contain("external_load");
        catalog.Confidence.Should().Be("high");
    }

    [Fact]
    public void ApplyUpdatesExistingTaxonomyChildInsteadOfCreatingASecondOne()
    {
        var catalog = ExerciseCatalog.Create(BuildSnapshot(movementPattern: "squat"), UtcNow);
        var originalTaxonomyId = catalog.Taxonomy!.Id;

        catalog.Apply(BuildSnapshot(movementPattern: "hinge"), UtcNow.AddDays(1));

        catalog.Taxonomy!.Id.Should().Be(originalTaxonomyId);
        catalog.MovementPattern.Should().Be("hinge");
    }

    [Fact]
    public void CanBeApprovedReturnsFalseWhenOnlyVideoOrImageArePresentButNoGif()
    {
        var withVideoOnly = ExerciseCatalog.Create(BuildSnapshot(videoUrl: "https://video.example/ex.mp4"), UtcNow);
        var withImageOnly = ExerciseCatalog.Create(BuildSnapshot(imageUrl: "https://cdn.example/ex.jpg"), UtcNow);
        var withNoMedia = ExerciseCatalog.Create(BuildSnapshot(), UtcNow);

        // RN-006 (US-236): a partir desta US, nenhum exercicio pode ser aprovado sem GIF 360,
        // mesmo que video ou imagem estaticos estejam disponiveis (comportamento antigo aceitava qualquer um).
        withVideoOnly.CanBeApproved().Should().BeFalse();
        withImageOnly.CanBeApproved().Should().BeFalse();
        withNoMedia.CanBeApproved().Should().BeFalse();
    }

    [Fact]
    public void CanBeApprovedReturnsTrueWhenGifUrlIsPresentAlongsideOtherRequiredData()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif"), UtcNow);
        catalog.SetAttributeContribution(
            ExerciseAttributeContribution.CreateAutoGenerated("strength", 2, 0, 0, 0, 0, 1),
            UtcNow);

        catalog.CanBeApproved().Should().BeTrue();
    }

    [Fact]
    public void ApproveForWorkoutGenerationThrowsWhenGifUrlIsMissingEvenWithVideoUrl()
    {
        var catalog = ExerciseCatalog.Create(BuildSnapshot(videoUrl: "https://video.example/ex.mp4"), UtcNow);
        catalog.SetAttributeContribution(
            ExerciseAttributeContribution.CreateAutoGenerated("strength", 2, 0, 0, 0, 0, 1),
            UtcNow);

        var act = () => catalog.ApproveForWorkoutGeneration(UtcNow);

        act.Should().Throw<InvalidOperationException>();
        catalog.IsApprovedForWorkoutGeneration.Should().BeFalse();
    }

    [Fact]
    public void ApproveForWorkoutGenerationSucceedsWhenGifUrlIsPresent()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif"), UtcNow);
        catalog.SetAttributeContribution(
            ExerciseAttributeContribution.CreateAutoGenerated("strength", 2, 0, 0, 0, 0, 1),
            UtcNow);

        catalog.ApproveForWorkoutGeneration(UtcNow);

        catalog.IsApprovedForWorkoutGeneration.Should().BeTrue();
        catalog.SanitizationStatus.Should().Be("approved");
    }

    /// <summary>
    /// R3.2 (US-148) — <see cref="ExerciseCatalog.SanitizationIssues"/> é calculado dentro de
    /// <see cref="ExerciseCatalog.Apply"/> a partir dos próprios campos do snapshot (RN-002/RN-005/RN-006/RN-007
    /// do US-148) e passa a bloquear <see cref="ExerciseCatalog.CanBeApproved"/> quando não está vazio (RN-009).
    /// </summary>
    [Fact]
    public void SanitizationIssuesContainsEquipmentUnmappedWhenRequiredEquipmentIsEmpty()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif", requiredEquipment: []), UtcNow);
        catalog.SetAttributeContribution(
            ExerciseAttributeContribution.CreateAutoGenerated("strength", 2, 0, 0, 0, 0, 1),
            UtcNow);

        catalog.SanitizationIssues.Should().Contain("equipment_unmapped");
        catalog.CanBeApproved().Should().BeFalse();
    }

    [Fact]
    public void SanitizationIssuesContainsEquipmentUnmappedWhenEquipmentValueIsNotAKnownEquipmentType()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(
                gifUrl: "https://cdn.awaken.app/0001/360.gif",
                requiredEquipment: ["some_exotic_gadget"]),
            UtcNow);

        catalog.SanitizationIssues.Should().Contain("equipment_unmapped");
    }

    [Fact]
    public void SanitizationIssuesContainsDifficultyOutOfRangeWhenDifficultyRankIsBelow1()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif", difficultyRank: 0), UtcNow);

        catalog.SanitizationIssues.Should().Contain("difficulty_out_of_range");
    }

    [Fact]
    public void SanitizationIssuesContainsDifficultyOutOfRangeWhenDifficultyRankIsAbove5()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif", difficultyRank: 6), UtcNow);

        catalog.SanitizationIssues.Should().Contain("difficulty_out_of_range");
    }

    [Fact]
    public void SanitizationIssuesContainsMissingGoalTagsWhenGoalTagsIsEmpty()
    {
        // Nota de regressao (ver plano R3.2): hoje BuildGoalTags sempre retorna "maintenance" no
        // handler de import, entao este caso nao acontece na pratica — este teste e uma guarda
        // caso essa garantia mude no futuro.
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif", goalTags: []), UtcNow);

        catalog.SanitizationIssues.Should().Contain("missing_goal_tags");
    }

    [Fact]
    public void SanitizationIssuesContainsMissingPrimaryMuscleWhenPrimaryMuscleGroupsIsEmpty()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif", primaryMuscleGroups: []), UtcNow);

        catalog.SanitizationIssues.Should().Contain("missing_primary_muscle");
    }

    [Fact]
    public void SanitizationIssuesIsEmptyForAFullyValidExerciseAndCanBeApprovedStillWorks()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif"), UtcNow);
        catalog.SetAttributeContribution(
            ExerciseAttributeContribution.CreateAutoGenerated("strength", 2, 0, 0, 0, 0, 1),
            UtcNow);

        catalog.SanitizationIssues.Should().BeEmpty();
        catalog.CanBeApproved().Should().BeTrue();
    }

    /// <summary>
    /// R3.3 (US-149) — <see cref="ExerciseCatalog.Reject"/> e o parâmetro opcional <c>reviewedBy</c> de
    /// <see cref="ExerciseCatalog.ApproveForWorkoutGeneration"/> dão suporte à curadoria manual
    /// (RN-005/RN-006 do US-149), distinguindo aprovação automática do import (sem revisor) da aprovação
    /// manual de um curador (com revisor e timestamp de revisão gravados).
    /// </summary>
    [Fact]
    public void RejectSetsSanitizationStatusRejectedAndClearsApprovalFlagWithAuditTrail()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif"), UtcNow);

        catalog.Reject("sem sentido", "curator@awaken.app", UtcNow.AddHours(1));

        catalog.SanitizationStatus.Should().Be("rejected");
        catalog.IsApprovedForWorkoutGeneration.Should().BeFalse();
        catalog.RejectionReason.Should().Be("sem sentido");
        catalog.ReviewedBy.Should().Be("curator@awaken.app");
        catalog.ReviewedAtUtc.Should().Be(UtcNow.AddHours(1));
    }

    [Fact]
    public void RejectCanReverseAPreviouslyApprovedExercise()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif"), UtcNow);
        catalog.SetAttributeContribution(
            ExerciseAttributeContribution.CreateAutoGenerated("strength", 2, 0, 0, 0, 0, 1),
            UtcNow);
        catalog.ApproveForWorkoutGeneration(UtcNow);

        catalog.Reject("virou duplicado", "curator@awaken.app", UtcNow.AddDays(1));

        catalog.IsApprovedForWorkoutGeneration.Should().BeFalse();
        catalog.SanitizationStatus.Should().Be("rejected");
    }

    [Fact]
    public void ApproveForWorkoutGenerationRecordsReviewedByAndReviewedAtWhenProvidedByACurator()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif"), UtcNow);
        catalog.SetAttributeContribution(
            ExerciseAttributeContribution.CreateAutoGenerated("strength", 2, 0, 0, 0, 0, 1),
            UtcNow);

        catalog.ApproveForWorkoutGeneration(UtcNow.AddHours(2), reviewedBy: "curator@awaken.app");

        catalog.IsApprovedForWorkoutGeneration.Should().BeTrue();
        catalog.ReviewedBy.Should().Be("curator@awaken.app");
        catalog.ReviewedAtUtc.Should().Be(UtcNow.AddHours(2));
    }

    [Fact]
    public void ApproveForWorkoutGenerationLeavesReviewedByNullForAutomaticImportApproval()
    {
        var catalog = ExerciseCatalog.Create(
            BuildSnapshot(gifUrl: "https://cdn.awaken.app/0001/360.gif"), UtcNow);
        catalog.SetAttributeContribution(
            ExerciseAttributeContribution.CreateAutoGenerated("strength", 2, 0, 0, 0, 0, 1),
            UtcNow);

        catalog.ApproveForWorkoutGeneration(UtcNow);

        catalog.IsApprovedForWorkoutGeneration.Should().BeTrue();
        catalog.ReviewedBy.Should().BeNull();
    }
}
