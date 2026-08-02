using Awaken.Api.Controllers.V1;
using Awaken.Application.Auth.Commands.DeleteAccount;
using Awaken.Application.Auth.Commands.GoogleSignIn;
using Awaken.Application.Auth.Commands.Login;
using Awaken.Application.Auth.Commands.Logout;
using Awaken.Application.Auth.Commands.Register;
using Awaken.Application.Auth.Commands.RefreshToken;
using Awaken.Contracts.Auth;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Awaken.UnitTests.Api;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediator = new();

    [Fact]
    public async Task Register_MapsNameBasedRequest_AndReturnsCreatedResult()
    {
        var response = new AuthResponse(
            "access-token",
            "refresh-token",
            new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc),
            new UserDto(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "hunter@awaken.app",
                "Hunter",
                null,
                "pt-BR",
                false,
                null));

        _mediator.Setup(m => m.Send(
                It.IsAny<RegisterUserCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new AuthController(_mediator.Object);

        var result = await controller.Register(
            new RegisterUserRequest("Hunter", "hunter@awaken.app", "Str0ngPass!", "pt-BR"),
            CancellationToken.None);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.Value.Should().BeSameAs(response);

        _mediator.Verify(m => m.Send(
            It.Is<RegisterUserCommand>(command =>
                command.Email == "hunter@awaken.app" &&
                command.Password == "Str0ngPass!" &&
                command.DisplayName == "Hunter" &&
                command.Language == "pt-BR"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_SendsCommand_AndReturnsOkResult()
    {
        var response = new AuthResponse(
            "access-token",
            "refresh-token",
            new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc),
            new UserDto(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "hunter@awaken.app",
                "Hunter",
                null,
                "pt-BR",
                false,
                null));

        _mediator.Setup(m => m.Send(
                It.IsAny<LoginUserCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new AuthController(_mediator.Object);

        var result = await controller.Login(
            new LoginUserRequest("hunter@awaken.app", "Str0ngPass!"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);

        _mediator.Verify(m => m.Send(
            It.Is<LoginUserCommand>(command =>
                command.Email == "hunter@awaken.app" &&
                command.Password == "Str0ngPass!"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_SendsCommand_AndReturnsOkResult()
    {
        var response = new AuthResponse(
            "access-token",
            "refresh-token",
            new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc),
            new UserDto(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "hunter@awaken.app",
                "Hunter",
                null,
                "pt-BR",
                false,
                null));

        _mediator.Setup(m => m.Send(
                It.IsAny<RefreshTokenCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new AuthController(_mediator.Object);

        var result = await controller.RefreshToken(
            new RefreshTokenRequest("refresh-token-value"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);

        _mediator.Verify(m => m.Send(
            It.Is<RefreshTokenCommand>(command =>
                command.RefreshToken == "refresh-token-value"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_SendsCommand_AndReturnsOkResult()
    {
        _mediator.Setup(m => m.Send(
                It.IsAny<LogoutCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "logout-corr-unit";
        var controller = new AuthController(_mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.Logout(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            success = true,
            correlationId = "logout-corr-unit"
        });
        _mediator.Verify(m => m.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Google_SendsCommand_AndReturnsOkResult()
    {
        var response = new AuthResponse(
            "access-token",
            "refresh-token",
            new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc),
            new UserDto(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "hunter@awaken.app",
                "Hunter",
                null,
                "pt-BR",
                false,
                null));

        _mediator.Setup(m => m.Send(
                It.IsAny<GoogleSignInCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new AuthController(_mediator.Object);

        var result = await controller.Google(
            new GoogleSignInRequest("google", "valid-id-token"),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);

        _mediator.Verify(m => m.Send(
            It.Is<GoogleSignInCommand>(command =>
                command.Provider == "google" &&
                command.ProviderCredential == "valid-id-token"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_SendsCommand_AndReturnsOkResult()
    {
        _mediator.Setup(m => m.Send(
                It.IsAny<DeleteAccountCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = "delete-account-corr-unit";
        var controller = new AuthController(_mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.DeleteAccount(
            new DeleteAccountRequest(DeleteAccountRequest.ExpectedConfirmation),
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            success = true,
            accountStatus = "deleted",
            correlationId = "delete-account-corr-unit"
        });
        _mediator.Verify(m => m.Send(It.IsAny<DeleteAccountCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_ReturnsBadRequest_WhenConfirmationIsInvalid()
    {
        var controller = new AuthController(_mediator.Object);

        var result = await controller.DeleteAccount(
            new DeleteAccountRequest("WRONG_CONFIRMATION"),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediator.Verify(m => m.Send(It.IsAny<DeleteAccountCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
