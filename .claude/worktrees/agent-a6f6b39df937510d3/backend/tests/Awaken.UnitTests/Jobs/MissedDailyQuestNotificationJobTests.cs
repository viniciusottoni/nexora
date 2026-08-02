using Awaken.Application.Notifications.Commands.SendMissedDailyQuestNotification;
using Awaken.Infrastructure.Jobs;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Jobs;

public class MissedDailyQuestNotificationJobTests
{
    private readonly Mock<IMediator> _mediator = new();

    [Fact]
    public async Task RunAsync_SendsSendMissedDailyQuestNotificationCommand()
    {
        using var cts = new CancellationTokenSource();

        await new MissedDailyQuestNotificationJob(_mediator.Object).RunAsync(cts.Token);

        _mediator.Verify(
            m => m.Send(
                It.IsAny<SendMissedDailyQuestNotificationCommand>(),
                cts.Token),
            Times.Once);
        _mediator.VerifyNoOtherCalls();
    }
}
