using Awaken.Application.Common.Interfaces;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Awaken.UnitTests.Nutrition;

public class UserDateServiceTests
{
    private static UserDateService CreateService(DateTime utcNow, string? headerValue = null)
    {
        var context = new DefaultHttpContext();
        if (headerValue is not null)
        {
            context.Request.Headers["X-Timezone-Offset-Minutes"] = headerValue;
        }

        var accessor = new HttpContextAccessor { HttpContext = context };
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(s => s.UtcNow).Returns(utcNow);

        return new UserDateService(accessor, dateTimeService.Object);
    }

    [Fact]
    public void TodayLocal_UsesClientTimezoneOffset_WhenHeaderIsValid()
    {
        var service = CreateService(
            new DateTime(2026, 6, 27, 22, 30, 0, DateTimeKind.Utc),
            "180");

        service.TodayLocal.Should().Be(new DateOnly(2026, 6, 28));
    }

    [Fact]
    public void TodayLocal_FallsBackToUtc_WhenHeaderIsMissing()
    {
        var service = CreateService(new DateTime(2026, 6, 27, 22, 30, 0, DateTimeKind.Utc));

        service.TodayLocal.Should().Be(new DateOnly(2026, 6, 27));
    }

    [Fact]
    public void TodayLocal_FallsBackToUtc_WhenHeaderIsInvalid()
    {
        var service = CreateService(
            new DateTime(2026, 6, 27, 22, 30, 0, DateTimeKind.Utc),
            "banana");

        service.TodayLocal.Should().Be(new DateOnly(2026, 6, 27));
    }
}
