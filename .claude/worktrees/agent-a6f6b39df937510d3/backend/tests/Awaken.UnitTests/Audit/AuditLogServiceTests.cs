using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Awaken.UnitTests.Audit;

public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _repository = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly DateTime _utcNow = new(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc);

    private AuditLogService CreateService() => new(
        _repository.Object,
        _dateTimeService.Object,
        _httpContextAccessor.Object);

    public AuditLogServiceTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(_utcNow);
    }

    [Fact]
    public async Task RecordAsyncAddsAuditLogWithCorrectFields()
    {
        var actorId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        AuditLog? captured = null;

        _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((entry, _) => captured = entry)
            .Returns(Task.CompletedTask);

        await CreateService().RecordAsync(
            "account_deleted",
            actorId,
            AuditActorType.User,
            "User",
            resourceId,
            null,
            CancellationToken.None);

        _repository.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        captured.Should().NotBeNull();
        captured!.Action.Should().Be("account_deleted");
        captured.ActorUserId.Should().Be(actorId);
        captured.ActorType.Should().Be(AuditActorType.User);
        captured.ResourceType.Should().Be("User");
        captured.ResourceId.Should().Be(resourceId);
        captured.MetadataSafe.Should().BeNull();
        captured.CreatedAtUtc.Should().Be(_utcNow);
    }

    [Fact]
    public async Task RecordAsyncCapturesCorrelationIdFromHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "test-correlation-id";
        _httpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        AuditLog? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((entry, _) => captured = entry)
            .Returns(Task.CompletedTask);

        await CreateService().RecordAsync(
            "legal_terms_accepted",
            Guid.NewGuid(),
            AuditActorType.User,
            "User",
            null,
            "{\"termsVersion\":\"v1.0\"}",
            CancellationToken.None);

        captured!.CorrelationId.Should().Be("test-correlation-id");
        captured.MetadataSafe.Should().Be("{\"termsVersion\":\"v1.0\"}");
    }

    [Fact]
    public async Task RecordAsyncSetsNullCorrelationIdWhenNoHttpContext()
    {
        _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        AuditLog? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((entry, _) => captured = entry)
            .Returns(Task.CompletedTask);

        await CreateService().RecordAsync(
            "trial_started",
            Guid.NewGuid(),
            AuditActorType.User,
            "Subscription",
            null,
            null,
            CancellationToken.None);

        captured!.CorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task RecordAsyncSupportsNullActorUserIdForSystemActions()
    {
        _httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        AuditLog? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((entry, _) => captured = entry)
            .Returns(Task.CompletedTask);

        await CreateService().RecordAsync(
            "system_event",
            null,
            AuditActorType.System,
            "System",
            null,
            null,
            CancellationToken.None);

        captured!.ActorUserId.Should().BeNull();
        captured.ActorType.Should().Be(AuditActorType.System);
    }
}
