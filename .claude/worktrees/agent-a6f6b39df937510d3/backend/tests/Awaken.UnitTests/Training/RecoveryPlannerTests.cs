using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Training;
using Awaken.Domain.Services.Training;
using FluentAssertions;

namespace Awaken.UnitTests.Training;

// US-239: RecoveryPlanner é o serviço de domínio puro que aplica as tabelas
// científicas de recuperação (janela 24/48/72h por intensidade, teto de
// volume semanal por nível efetivo). Testes nomeados por CA/RN da US-239.
public class RecoveryPlannerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc);

    private static MuscleRecoveryState BuildState(string intensity, double hoursAgo)
    {
        var state = MuscleRecoveryState.CreateInitial(UserId, MuscleGroups.Quadriceps, UtcNow.AddHours(-hoursAgo));
        state.RegisterSession(UtcNow.AddHours(-hoursAgo), intensity, ["squat_family"], setsPerformed: 4);
        return state;
    }

    // CA-001 (US-239): pernas treinadas pesado há 40h (dentro da janela de
    // 72h, mas já passou de metade dela, > 36h) => Recovering, volume/RPE rebaixados.
    [Fact]
    public void CA001_PernasPesadoOntem_VolumeRebaixado()
    {
        var state = BuildState("heavy", hoursAgo: 40);

        var status = RecoveryPlanner.StatusFor(state, UtcNow);
        var (volumeCapFactor, rpeCapDelta) = RecoveryPlanner.ModulationFor(status);

        status.Should().Be(MuscleRecoveryStatus.Recovering);
        volumeCapFactor.Should().Be(0.5m);
        rpeCapDelta.Should().Be(-1);
    }

    // RN-001: janela leve (24h) já passou (30h) => grupo recuperado.
    [Fact]
    public void RN001_JanelaLeve24h_JaPassou_Recuperado()
    {
        var state = BuildState("light", hoursAgo: 30);

        var status = RecoveryPlanner.StatusFor(state, UtcNow);

        status.Should().Be(MuscleRecoveryStatus.Recovered);
    }

    // RN-003: treino pesado há só 10h (< 50% de 72h) => fadigado, cap mínimo.
    [Fact]
    public void RN003_Fadigado_CapMinimo()
    {
        var state = BuildState("heavy", hoursAgo: 10);

        var status = RecoveryPlanner.StatusFor(state, UtcNow);
        var (volumeCapFactor, rpeCapDelta) = RecoveryPlanner.ModulationFor(status);

        status.Should().Be(MuscleRecoveryStatus.Fatigued);
        volumeCapFactor.Should().Be(0.25m);
        rpeCapDelta.Should().Be(-2);
    }

    // CA-004: teto de volume semanal recuperável por nível — avançado vai até 20.
    [Fact]
    public void CA004_TetoSemanal_Avancado()
    {
        RecoveryPlanner.WeeklySetCapFor("advanced").Should().Be(20);
    }

    // RN 10.4: sem histórico prévio (retorno após folga longa/1a sessão),
    // o grupo é considerado recuperado, sem penalidade.
    [Fact]
    public void SemHistorico_ConsideraRecuperado()
    {
        var status = RecoveryPlanner.StatusFor(null, UtcNow);

        status.Should().Be(MuscleRecoveryStatus.Recovered);
    }

    // US-239 §13: intensidade inferida do catálogo — impacto alto ou
    // dificuldade >= 3 conta como pesado, dificuldade 0 como leve, senão moderado.
    [Theory]
    [InlineData(4, 1, "heavy")] // impacto alto
    [InlineData(1, 3, "heavy")] // dificuldade alta
    [InlineData(1, 0, "light")]
    [InlineData(1, 1, "moderate")]
    public void IntensityFor_InfersFromImpactAndDifficultyRank(int impactLevel, int difficultyRank, string expected)
    {
        var snapshot = BuildCatalogSnapshotWith(impactLevel, difficultyRank);
        var exercise = ExerciseCatalog.Create(snapshot, UtcNow);

        RecoveryPlanner.IntensityFor(exercise).Should().Be(expected);
    }

    private static ExerciseCatalogSnapshot BuildCatalogSnapshotWith(int impactLevel, int difficultyRank) => new(
        RawImportId: null,
        ProviderName: "local_files",
        ProviderExerciseId: "0001",
        ProviderVersion: null,
        NamePtBr: "Agachamento",
        NameOriginal: "Squat",
        Slug: "agachamento",
        DescriptionPtBr: null,
        InstructionsPtBr: ["Passo 1"],
        InstructionsOriginal: ["Step 1"],
        TipsPtBr: [],
        ExerciseType: "strength",
        MovementPattern: "squat",
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
        ImpactLevel: impactLevel,
        Environment: "gym",
        RequiredEquipment: ["barbell"],
        PrimaryMuscleGroups: ["quadriceps"],
        SecondaryMuscleGroups: ["glutes"],
        BodyParts: ["legs"],
        JointStressTags: [],
        ContraindicationTags: [],
        LimitationBlockTags: [],
        PainBlockTags: [],
        GoalTags: [],
        RiskTags: [],
        AccessibilityTags: [],
        TaxonomySignals: [],
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
        VideoUrl: null,
        ImageUrl: null,
        GifUrl: "https://cdn.awaken.app/0001/360.gif",
        MediaLicenseInfo: null,
        SanitizationStatus: "pending_review",
        IsApprovedForWorkoutGeneration: false,
        Confidence: "high");
}
