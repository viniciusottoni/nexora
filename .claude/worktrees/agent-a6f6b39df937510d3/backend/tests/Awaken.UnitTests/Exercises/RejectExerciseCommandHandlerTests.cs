using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Exercises.Commands.RejectExercise;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Exercises;

/// <summary>US-149 (R3.3) — RN-005: qualquer exercício pode ser reprovado por um curador, com motivo
/// obrigatório e trilha de auditoria (quem reprovou e quando).</summary>
public class RejectExerciseCommandHandlerTests
{
    private readonly Mock<IExerciseCatalogRepository> _catalogRepository = new();
    private readonly Mock<IExerciseRawImportRepository> _rawImportRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<RejectExerciseCommandHandler>> _logger = new();

    private static readonly DateTime UtcNow = new(2026, 7, 5, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid CuratorId = Guid.NewGuid();

    public RejectExerciseCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
        _currentUserService.Setup(c => c.UserId).Returns(CuratorId);
        _currentUserService.Setup(c => c.Email).Returns("curator@awaken.app");
    }

    private RejectExerciseCommandHandler CreateHandler() => new(
        _catalogRepository.Object,
        _rawImportRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static ExerciseCatalogSnapshot MinimalSnapshot(Guid? rawImportId = null) => new(
        RawImportId: rawImportId,
        ProviderName: "local_files",
        ProviderExerciseId: "0001",
        ProviderVersion: null,
        NamePtBr: "Exercicio invalido",
        NameOriginal: "Invalid exercise",
        Slug: "exercicio-invalido",
        DescriptionPtBr: null,
        InstructionsPtBr: [],
        InstructionsOriginal: [],
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
        DifficultyRank: 3,
        TechnicalComplexity: 3,
        ImpactLevel: 2,
        Environment: "gym",
        RequiredEquipment: [],
        PrimaryMuscleGroups: [],
        SecondaryMuscleGroups: [],
        BodyParts: [],
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
        GifUrl: null,
        MediaLicenseInfo: null,
        SanitizationStatus: "pending_review",
        IsApprovedForWorkoutGeneration: false,
        Confidence: "low");

    [Fact]
    public async Task HandleRejectsExerciseAndRecordsReasonAndReviewer()
    {
        var catalog = ExerciseCatalog.Create(MinimalSnapshot(), UtcNow);
        _catalogRepository.Setup(r => r.GetByIdAsync(catalog.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        var result = await CreateHandler().Handle(
            new RejectExerciseCommand(catalog.Id, "sem sentido"), CancellationToken.None);

        catalog.SanitizationStatus.Should().Be("rejected");
        catalog.IsApprovedForWorkoutGeneration.Should().BeFalse();
        catalog.RejectionReason.Should().Be("sem sentido");
        catalog.ReviewedBy.Should().Be("curator@awaken.app");
        result.Id.Should().Be(catalog.Id);
        result.Status.Should().Be("rejected");
        result.RejectionReason.Should().Be("sem sentido");
        _catalogRepository.Verify(r => r.Update(catalog), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAlsoMarksAssociatedRawImportAsRejectedWhenItExists()
    {
        var rawImport = ExerciseRawImport.CreateImported(
            "local_files", "0001", null, "{}", "imp_1", null, null, null, null, UtcNow);
        var catalog = ExerciseCatalog.Create(MinimalSnapshot(rawImportId: rawImport.Id), UtcNow);
        _catalogRepository.Setup(r => r.GetByIdAsync(catalog.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);
        _rawImportRepository.Setup(r => r.GetByIdAsync(rawImport.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawImport);

        await CreateHandler().Handle(new RejectExerciseCommand(catalog.Id, "duplicado"), CancellationToken.None);

        rawImport.Status.Should().Be(ExerciseRawImportStatus.Rejected);
        rawImport.ErrorMessage.Should().Be("duplicado");
        _rawImportRepository.Verify(r => r.Update(rawImport), Times.Once);
    }

    [Fact]
    public async Task HandleThrowsNotFoundWhenExerciseDoesNotExist()
    {
        var missingId = Guid.NewGuid();
        _catalogRepository.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExerciseCatalog?)null);

        var act = async () => await CreateHandler().Handle(
            new RejectExerciseCommand(missingId, "motivo"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
