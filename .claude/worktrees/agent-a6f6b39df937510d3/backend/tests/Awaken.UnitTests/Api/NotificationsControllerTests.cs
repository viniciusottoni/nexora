using Awaken.Api.Controllers.V1;
using Awaken.Application.Notifications.Commands.UpdatePreferences;
using Awaken.Application.Notifications.Commands.UpdateReminderTime;
using Awaken.Application.Notifications.Queries.GetPreferences;
using Awaken.Contracts.Notifications;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Awaken.UnitTests.Api;

public class NotificationsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();

    [Fact]
    public async Task UpdatePreferences_MapsRequest_AndReturnsOkResult()
    {
        var response = new NotificationPreferencesResponse(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            true,
            "fcm-token-xyz",
            "token_registered",
            null,
            null,
            new DateTime(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc));

        _mediator.Setup(m => m.Send(
                It.IsAny<UpdateNotificationPreferencesCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new NotificationsController(_mediator.Object);

        var result = await controller.UpdatePreferences(
            new UpdateNotificationPreferencesRequest(
                true,
                "fcm-token-xyz",
                "token_registered"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);

        _mediator.Verify(m => m.Send(
            It.Is<UpdateNotificationPreferencesCommand>(command =>
                command.PushEnabled == true &&
                command.PushToken == "fcm-token-xyz" &&
                command.PermissionStatus == "token_registered"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPreferences_ReturnsOkWhenPreferenceExists()
    {
        var response = new NotificationPreferencesResponse(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            true,
            "fcm-token-abc",
            "granted",
            null,
            null,
            new DateTime(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc));

        _mediator.Setup(m => m.Send(
                It.IsAny<GetNotificationPreferencesQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new NotificationsController(_mediator.Object);

        var result = await controller.GetPreferences(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task GetPreferences_ReturnsNotFoundWhenPreferenceMissing()
    {
        _mediator.Setup(m => m.Send(
                It.IsAny<GetNotificationPreferencesQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreferencesResponse?)null);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "notif-corr-unit";
        var controller = new NotificationsController(_mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.GetPreferences(CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new
        {
            code = "PREFERENCES_NOT_FOUND",
            message = "Nenhuma preferência de notificação configurada.",
            correlationId = "notif-corr-unit"
        });
    }

    [Fact]
    public async Task UpdateReminderTime_WithValidTime_ReturnsOk()
    {
        var response = new NotificationPreferencesResponse(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            false,
            null,
            "not_requested",
            new TimeOnly(19, 30),
            "America/Recife",
            new DateTime(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc));

        _mediator.Setup(m => m.Send(
                It.IsAny<UpdateReminderTimeCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new NotificationsController(_mediator.Object);

        var result = await controller.UpdateReminderTime(
            new UpdateReminderTimeRequest("19:30", "America/Recife"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);

        _mediator.Verify(m => m.Send(
            It.Is<UpdateReminderTimeCommand>(command =>
                command.PreferredReminderTime == new TimeOnly(19, 30) &&
                command.Timezone == "America/Recife"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("7:30")]
    [InlineData("19:30:00")]
    [InlineData("not-a-time")]
    [InlineData("25:00")]
    [InlineData("")]
    public async Task UpdateReminderTime_WithInvalidTimeFormat_ReturnsUnprocessableEntity(string invalidTime)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "notif-corr-unit";
        var controller = new NotificationsController(_mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.UpdateReminderTime(
            new UpdateReminderTimeRequest(invalidTime, "America/Recife"),
            CancellationToken.None);

        var unprocessable = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessable.Value.Should().BeEquivalentTo(new
        {
            code = "INVALID_TIME_FORMAT",
            message = "Time must be in HH:mm format.",
            correlationId = "notif-corr-unit"
        });

        _mediator.Verify(m => m.Send(It.IsAny<UpdateReminderTimeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateReminderTime_WithInvalidTimeFormat_GeneratesCorrelationIdWhenMissing()
    {
        var controller = new NotificationsController(_mediator.Object);

        var result = await controller.UpdateReminderTime(
            new UpdateReminderTimeRequest("not-a-time", "America/Recife"),
            CancellationToken.None);

        var unprocessable = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        var correlationId = unprocessable.Value!.GetType().GetProperty("correlationId")!.GetValue(unprocessable.Value) as string;

        correlationId.Should().NotBeNullOrWhiteSpace();
        _mediator.Verify(m => m.Send(It.IsAny<UpdateReminderTimeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
