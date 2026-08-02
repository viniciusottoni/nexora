using Awaken.Application.Admin.Readiness.Queries.GetReadinessStatus;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Readiness;
using Awaken.Shared.Admin;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.Readiness;

/// <summary>
/// US-218 — handler apenas delega ao IReadinessCheckService; cobre o contrato de delegação.
/// </summary>
public class GetReadinessStatusQueryHandlerTests
{
    private readonly Mock<IReadinessCheckService> _readinessCheckService = new();

    private GetReadinessStatusQueryHandler CreateHandler() => new(_readinessCheckService.Object);

    [Fact]
    public async Task Handle_DelegatesToReadinessCheckService_AndReturnsItsResult()
    {
        var expected = new ReadinessStatusResponse(
            [
                new EnvironmentReadinessResponse(
                    "prod",
                    DomainHealthStatus.Healthy,
                    false,
                    [],
                    DateTime.UtcNow),
            ],
            DateTime.UtcNow);

        _readinessCheckService
            .Setup(s => s.GetReadinessStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateHandler().Handle(new GetReadinessStatusQuery(), CancellationToken.None);

        result.Should().BeSameAs(expected);
        _readinessCheckService.Verify(s => s.GetReadinessStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
