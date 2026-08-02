using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Queries.GetQuestExecution;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class GetQuestExecutionQueryHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<IExerciseCatalogRepository> _exerciseCatalogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 6, 23, 9, 0, 0, DateTimeKind.Utc);

    public GetQuestExecutionQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    private GetQuestExecutionQueryHandler CreateHandler() => new(
        _questRepository.Object,
        _exerciseCatalogRepository.Object,
        _currentUserService.Object);

    // US-041 (R2.2): catalogo minimo usado para resolver InstructionsPtBr/TipsPtBr
    // a partir do ExerciseCatalogProviderId do QuestExercise (mesmo padrao usado
    // pela US-239 para resolver grupos musculares em CompleteQuestCommandHandler).
    private static ExerciseCatalog BuildCatalog(
        string providerExerciseId, List<string> instructions, List<string> tips) =>
        ExerciseCatalog.Create(
            new ExerciseCatalogSnapshot(
                RawImportId: null,
                ProviderName: "local_files",
                ProviderExerciseId: providerExerciseId,
                ProviderVersion: null,
                NamePtBr: "Agachamento",
                NameOriginal: "Squat",
                Slug: "agachamento",
                DescriptionPtBr: null,
                InstructionsPtBr: instructions,
                InstructionsOriginal: [],
                TipsPtBr: tips,
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
                DifficultyRank: 3,
                TechnicalComplexity: 3,
                ImpactLevel: 2,
                Environment: "gym",
                RequiredEquipment: ["barbell"],
                PrimaryMuscleGroups: ["quadriceps"],
                SecondaryMuscleGroups: [],
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
                Confidence: "high"),
            UtcNow);

    private static Quest BuildStartedQuestWithExercises(Guid userId, params string[] exerciseNames)
    {
        var quest = Quest.Create(userId, new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        var seeds = exerciseNames.Select(name => new QuestExerciseSeed(
            Name: name, ExerciseCatalogProviderId: null, Sets: 3, RepsMin: 8, RepsMax: 12,
            RestSeconds: 60, TargetRpe: "8", VideoUrl: null,
            XpReward: 10, StrengthXp: 1, AgilityXp: 0, EnduranceXp: 0, VitalityXp: 0, FocusXp: 0, WisdomXp: 1));
        quest.Start(UtcNow, seeds);
        return quest;
    }

    [Fact]
    public async Task ReturnsExercisesOrdered_WhenQuestInProgress()
    {
        var quest = BuildStartedQuestWithExercises(UserId, "Squat", "Push-up", "Plank");
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestExecutionQuery(quest.Id), CancellationToken.None);

        result.QuestId.Should().Be(quest.Id);
        result.Status.Should().Be("in_progress");
        result.AttributeXpPreview.Strength.Should().Be(9);
        result.AttributeXpPreview.Wisdom.Should().Be(3);
        result.Exercises.Should().HaveCount(3);
        result.Exercises.Select(e => e.Name).Should().ContainInOrder("Squat", "Push-up", "Plank");
        result.Exercises.Select(e => e.Order).Should().ContainInOrder(1, 2, 3);
        result.Exercises[0].EffectiveDifficulty.Should().Be(3);
        result.Exercises[0].AttributeImpacts.Strength.Should().Be(3);
        result.Exercises[0].HiddenAttributeImpacts.Wisdom.Should().Be(1);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("completed")]
    [InlineData("cancelled")]
    public async Task ThrowsConflict_WhenQuestIsNotInProgress(string status)
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        if (status != "pending")
        {
            typeof(Quest).GetProperty(nameof(Quest.Status))!.SetValue(quest, status);
        }
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new GetQuestExecutionQuery(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_NOT_IN_PROGRESS");
    }

    [Fact]
    public async Task ThrowsConflict_WhenQuestHasNoExercises()
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        quest.Start(UtcNow, []);
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new GetQuestExecutionQuery(quest.Id), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Code.Should().Be("QUEST_HAS_NO_EXERCISES");
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestBelongsToAnotherUser()
    {
        var quest = BuildStartedQuestWithExercises(Guid.NewGuid(), "Squat");
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var act = () => CreateHandler().Handle(new GetQuestExecutionQuery(quest.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsNotFound_WhenQuestDoesNotExist()
    {
        var questId = Guid.NewGuid();
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(questId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        var act = () => CreateHandler().Handle(new GetQuestExecutionQuery(questId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // US-041 (R2.2): instrucoes/dicas do exercicio nunca chegavam na tela de execucao
    // porque QuestExercise nao guarda esse dado - precisa ser resolvido em tempo de
    // consulta via ExerciseCatalogProviderId, mesmo padrao ja usado na US-239.
    [Fact]
    public async Task PopulatesInstructionsAndTips_WhenCatalogResolved()
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        var seed = new QuestExerciseSeed(
            Name: "Squat", ExerciseCatalogProviderId: "ex-1", Sets: 3, RepsMin: 8, RepsMax: 12,
            RestSeconds: 60, TargetRpe: "8", VideoUrl: null,
            XpReward: 10, StrengthXp: 1, AgilityXp: 0, EnduranceXp: 0, VitalityXp: 0, FocusXp: 0, WisdomXp: 1);
        quest.Start(UtcNow, [seed]);
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var catalog = BuildCatalog("ex-1", ["Pes afastados", "Desca o quadril"], ["Mantenha as costas retas"]);
        _exerciseCatalogRepository
            .Setup(r => r.GetByProviderExerciseIdAsync("ex-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        var result = await CreateHandler().Handle(new GetQuestExecutionQuery(quest.Id), CancellationToken.None);

        result.Exercises[0].Instructions.Should().BeEquivalentTo(["Pes afastados", "Desca o quadril"]);
        result.Exercises[0].Tips.Should().BeEquivalentTo(["Mantenha as costas retas"]);
    }

    [Fact]
    public async Task LeavesInstructionsAndTipsEmpty_WhenExerciseHasNoProviderCatalogId()
    {
        var quest = BuildStartedQuestWithExercises(UserId, "Squat");
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestExecutionQuery(quest.Id), CancellationToken.None);

        result.Exercises[0].Instructions.Should().BeEmpty();
        result.Exercises[0].Tips.Should().BeEmpty();
        _exerciseCatalogRepository.Verify(
            r => r.GetByProviderExerciseIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LeavesInstructionsAndTipsEmpty_WhenCatalogNotFound()
    {
        var quest = Quest.Create(UserId, new DateTime(2026, 6, 23, 0, 0, 0, DateTimeKind.Utc), "pt-BR", "key");
        var seed = new QuestExerciseSeed(
            Name: "Squat", ExerciseCatalogProviderId: "ex-missing", Sets: 3, RepsMin: 8, RepsMax: 12,
            RestSeconds: 60, TargetRpe: "8", VideoUrl: null,
            XpReward: 10, StrengthXp: 1, AgilityXp: 0, EnduranceXp: 0, VitalityXp: 0, FocusXp: 0, WisdomXp: 1);
        quest.Start(UtcNow, [seed]);
        _questRepository.Setup(r => r.GetByIdWithExercisesAsync(quest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);
        _exerciseCatalogRepository
            .Setup(r => r.GetByProviderExerciseIdAsync("ex-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseCatalog?)null);

        var result = await CreateHandler().Handle(new GetQuestExecutionQuery(quest.Id), CancellationToken.None);

        result.Exercises[0].Instructions.Should().BeEmpty();
        result.Exercises[0].Tips.Should().BeEmpty();
    }
}
