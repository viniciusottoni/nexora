using Awaken.Application.Notifications.Commands.SendStreakRiskAlert;
using Awaken.Infrastructure.Jobs;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Jobs;

public class StreakRiskAlertJobTests
{
    private readonly Mock<IMediator> _mediator = new();

    [Fact]
    public async Task RunAsync_DispatchesSendStreakRiskAlertCommand()
    {
        var expectedResult = new SendStreakRiskAlertResult(1, 1, 0);
        _mediator
            .Setup(m => m.Send(
                It.IsAny<SendStreakRiskAlertCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var job = new StreakRiskAlertJob(_mediator.Object);
        using var cts = new CancellationTokenSource();

        await job.RunAsync(cts.Token);

        _mediator.Verify(m => m.Send(
            It.Is<SendStreakRiskAlertCommand>(_ => true),
            cts.Token), Times.Once);
    }
}
