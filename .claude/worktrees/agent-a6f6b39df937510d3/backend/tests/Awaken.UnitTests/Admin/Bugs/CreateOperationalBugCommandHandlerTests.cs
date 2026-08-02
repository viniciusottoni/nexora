using Awaken.Application.Admin.Bugs.Commands.CreateOperationalBug;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Bugs;
using Awaken.Domain.Repositories;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace Awaken.UnitTests.Admin.Bugs;

public class CreateOperationalBugCommandHandlerTests
{
    private readonly Mock<IOperationalBugRepository> _operationalBugRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OccurredAtUtc = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);
    private readonly Guid _adminId = Guid.NewGuid();

    public CreateOperationalBugCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _currentAdminService.Setup(s => s.AdminUserId).Returns(_adminId);
    }

    private CreateOperationalBugCommandHandler CreateHandler() => new(
        _operationalBugRepository.Object,
        _currentAdminService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    private static CreateOperationalBugCommand CreateCommand() => new(
        "Crash no login",
        "high",
        "auth-service",
        "prod",
        "internal",
        OccurredAtUtc,
        "Stack trace anexado.",
        "corr-123",
        null,
        null);

    [Fact]
    public async Task HandlePersistsBugWithCorrectCreatedByAdminIdAndAudits()
    {
        var command = CreateCommand();

        var resultId = await CreateHandler().Handle(command, CancellationToken.None);

        resultId.Should().NotBe(Guid.Empty);

        _operationalBugRepository.Verify(r => r.AddAsync(
            It.Is<OperationalBug>(b =>
                b.Title == command.Title &&
                b.Severity == command.Severity &&
                b.Component == command.Component &&
                b.Environment == command.Environment &&
                b.Origin == command.Origin &&
                b.CreatedByAdminId == _adminId &&
                b.OccurredAtUtc == OccurredAtUtc),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminBugCreated,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.OperationalBug,
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleDoesNotIncludeFullDescriptionInAuditMetadata()
    {
        var command = CreateCommand();
        string? capturedMetadata = null;

        _auditLogService
            .Setup(a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<AuditActorType>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, AuditActorType, string, Guid?, string?, CancellationToken>(
                (_, _, _, _, _, metadata, _) => capturedMetadata = metadata);

        await CreateHandler().Handle(command, CancellationToken.None);

        capturedMetadata.Should().NotContain(command.Description!);
    }
}

public class CreateOperationalBugCommandValidatorTests
{
    private readonly CreateOperationalBugCommandValidator _validator = new();

    private static CreateOperationalBugCommand ValidCommand() => new(
        "Crash no login",
        "high",
        "auth-service",
        "prod",
        "internal",
        new DateTime(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc),
        null,
        null,
        null,
        null);

    [Fact]
    public void ValidatorRejectsMissingSeverity()
    {
        var command = ValidCommand() with { Severity = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Severity);
    }

    [Fact]
    public void ValidatorRejectsMissingEnvironment()
    {
        var command = ValidCommand() with { Environment = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Environment);
    }

    [Fact]
    public void ValidatorRejectsMissingOccurredAtUtc()
    {
        var command = ValidCommand() with { OccurredAtUtc = default };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OccurredAtUtc);
    }

    [Fact]
    public void ValidatorRejectsSecretLikeDescription()
    {
        var command = ValidCommand() with { Description = "user sent password=abc123" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void ValidatorAcceptsFullyValidCommand()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
