using Awaken.Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Awaken.UnitTests.Middlewares;

public class CorrelationIdMiddlewareTests
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    private static async Task<DefaultHttpContext> InvokeAsync(string? incomingCorrelationId = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (incomingCorrelationId is not null)
            context.Request.Headers[CorrelationIdHeader] = incomingCorrelationId;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        return context;
    }

    [Fact]
    public async Task WhenNoHeaderSent_GeneratesUuidAndStoresInItems()
    {
        var context = await InvokeAsync();

        var stored = context.Items["CorrelationId"] as string;
        stored.Should().NotBeNullOrEmpty();
        Guid.TryParse(stored, out _).Should().BeTrue();
    }

    [Fact]
    public async Task WhenHeaderSent_PropagatesExistingValue()
    {
        const string existingId = "my-correlation-id";

        var context = await InvokeAsync(existingId);

        var stored = context.Items["CorrelationId"] as string;
        stored.Should().Be(existingId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WhenHeaderSentButBlankOrWhitespace_GeneratesUuidAndDoesNotPropagateBlankValue(string blankCorrelationId)
    {
        var context = await InvokeAsync(blankCorrelationId);

        var stored = context.Items["CorrelationId"] as string;
        stored.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(stored, out _).Should().BeTrue();
        context.Response.Headers[CorrelationIdHeader].ToString().Should().Be(stored);
    }

    [Fact]
    public async Task AlwaysIncludesCorrelationIdInResponse()
    {
        var context = await InvokeAsync();

        context.Response.Headers[CorrelationIdHeader].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task WhenHeaderSent_ResponseIncludesSameCorrelationId()
    {
        const string existingId = "my-correlation-id";

        var context = await InvokeAsync(existingId);

        context.Response.Headers[CorrelationIdHeader].ToString().Should().Be(existingId);
    }
}
