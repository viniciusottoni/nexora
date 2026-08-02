using Awaken.Application.Common.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Awaken.UnitTests.Application;

// Must be public so Moq/Castle.Core can generate the ILogger<LoggingBehavior<TestRequest, Unit>> proxy
public sealed record LoggingBehaviorTestRequest : IRequest<Unit>;

public class LoggingBehaviorTests
{
    private static LoggingBehavior<LoggingBehaviorTestRequest, Unit> CreateBehavior(
        Mock<ILogger<LoggingBehavior<LoggingBehaviorTestRequest, Unit>>> logger,
        string? correlationId)
    {
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        if (correlationId is not null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Items["CorrelationId"] = correlationId;
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        }
        else
        {
            httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        }

        return new LoggingBehavior<LoggingBehaviorTestRequest, Unit>(logger.Object, httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task SuccessfulRequest_LogsStartAndEnd_WithCorrelationId()
    {
        const string correlationId = "test-corr-id";
        var loggerMock = new Mock<ILogger<LoggingBehavior<LoggingBehaviorTestRequest, Unit>>>();
        var behavior = CreateBehavior(loggerMock, correlationId);

        var result = await behavior.Handle(
            new LoggingBehaviorTestRequest(),
            ct => Task.FromResult(Unit.Value),
            CancellationToken.None);

        result.Should().Be(Unit.Value);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Handling") && v.ToString()!.Contains(correlationId)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Handled") && v.ToString()!.Contains(correlationId)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task FailingRequest_LogsError_WithCorrelationId_AndRethrows()
    {
        const string correlationId = "error-corr-id";
        var loggerMock = new Mock<ILogger<LoggingBehavior<LoggingBehaviorTestRequest, Unit>>>();
        var behavior = CreateBehavior(loggerMock, correlationId);
        var exception = new InvalidOperationException("something went wrong");

        var act = async () => await behavior.Handle(
            new LoggingBehaviorTestRequest(),
            ct => throw exception,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("something went wrong");

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error handling") && v.ToString()!.Contains(correlationId)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task WithoutHttpContext_DoesNotThrow_LogsWithNullCorrelationId()
    {
        var loggerMock = new Mock<ILogger<LoggingBehavior<LoggingBehaviorTestRequest, Unit>>>();
        var behavior = CreateBehavior(loggerMock, correlationId: null);

        var act = async () => await behavior.Handle(
            new LoggingBehaviorTestRequest(),
            ct => Task.FromResult(Unit.Value),
            CancellationToken.None);

        await act.Should().NotThrowAsync();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Handling")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
