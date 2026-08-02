using Awaken.Application.Admin.MvpHealth.Queries.GetMvpHealth;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.MvpHealth;
using Awaken.Shared.Admin;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Admin.MvpHealth;

/// <summary>
/// US-216 — handler apenas delega ao IMvpHealthService; cobre o contrato de delegação.
/// </summary>
public class GetMvpHealthQueryHandlerTests
{
    private readonly Mock<IMvpHealthService> _mvpHealthService = new();

    private GetMvpHealthQueryHandler CreateHandler() => new(_mvpHealthService.Object);

    private static MvpHealthStatusResponse BuildResponse() =>
        new(
            DomainHealthStatus.Healthy,
            [
                new DomainCardResponse("security", "Segurança", DomainHealthStatus.Healthy,
                    "Nenhum alerta.", "/admin/security", DateTime.UtcNow),
            ],
            [],
            DateTime.UtcNow);

    [Fact]
    public async Task Handle_DelegatesToMvpHealthService_AndReturnsItsResult()
    {
        var expected = BuildResponse();

        _mvpHealthService
            .Setup(s => s.GetMvpHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateHandler().Handle(new GetMvpHealthQuery(), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_CallsServiceExactlyOnce()
    {
        _mvpHealthService
            .Setup(s => s.GetMvpHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponse());

        await CreateHandler().Handle(new GetMvpHealthQuery(), CancellationToken.None);

        _mvpHealthService.Verify(s => s.GetMvpHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnedResponse_HasExpectedShape()
    {
        var expected = BuildResponse();

        _mvpHealthService
            .Setup(s => s.GetMvpHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateHandler().Handle(new GetMvpHealthQuery(), CancellationToken.None);

        result.OverallStatus.Should().NotBeNullOrWhiteSpace();
        result.Domains.Should().NotBeNull();
        result.P0Blockers.Should().NotBeNull();
    }
}
