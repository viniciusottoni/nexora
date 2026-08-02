using Awaken.Application.Notifications.Commands.SendTrialEndingNotification;
using Awaken.Infrastructure.Jobs;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Jobs;

public class TrialEndingNotificationJobTests
{
    private readonly Mock<IMediator> _mediator = new();

    private TrialEndingNotificationJob CreateJob() => new(_mediator.Object);

    [Fact]
    public async Task RunAsync_SendsTrialEndingNotificationCommand()
    {
        using var cts = new CancellationTokenSource();

        await CreateJob().RunAsync(cts.Token);

        _mediator.Verify(
            m => m.Send(
                It.IsAny<SendTrialEndingNotificationCommand>(),
                cts.Token),
            Times.Once);
        _mediator.VerifyNoOtherCalls();
    }
}
