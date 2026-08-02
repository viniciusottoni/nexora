using Awaken.Application.Notifications.Commands.SendDailyQuestReminder;
using Awaken.Infrastructure.Jobs;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Jobs;

public class DailyQuestReminderJobTests
{
    private readonly Mock<IMediator> _mediator = new();

    private DailyQuestReminderJob CreateJob() => new(_mediator.Object);

    [Fact]
    public async Task RunAsync_SendsDailyQuestReminderCommand()
    {
        using var cts = new CancellationTokenSource();

        await CreateJob().RunAsync(cts.Token);

        _mediator.Verify(
            m => m.Send(
                It.IsAny<SendDailyQuestReminderCommand>(),
                cts.Token),
            Times.Once);
        _mediator.VerifyNoOtherCalls();
    }
}
