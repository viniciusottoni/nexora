using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Infrastructure;

public class EmailServiceTests
{
    [Fact]
    public async Task SendPasswordResetAsync_DoesNotWriteEmailOrTokenToLogs()
    {
        var logger = new Mock<ILogger<EmailService>>();
        var service = new EmailService(logger.Object);

        await service.SendPasswordResetAsync(
            "hunter@awaken.app",
            "raw-reset-token",
            CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    !state.ToString()!.Contains("hunter@awaken.app") &&
                    !state.ToString()!.Contains("raw-reset-token")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
