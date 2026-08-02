using Awaken.Application.Notifications.Commands.SendReactivationNotification;
using Awaken.Infrastructure.Jobs;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Jobs;

public class ReactivationNotificationJobTests
{
    private readonly Mock<IMediator> _mediator = new();

    private ReactivationNotificationJob CreateJob() => new(_mediator.Object);

    [Fact]
    public async Task RunAsync_SendsReactivationNotificationCommand()
    {
        using var cts = new CancellationTokenSource();

        await CreateJob().RunAsync(cts.Token);

        _mediator.Verify(
            m => m.Send(
                It.IsAny<SendReactivationNotificationCommand>(),
                cts.Token),
            Times.Once);
        _mediator.VerifyNoOtherCalls();
    }
}
