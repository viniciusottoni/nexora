using System.Security.Claims;
using Awaken.Application.Common.Exceptions;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Awaken.UnitTests.Infrastructure;

public class CurrentUserServiceTests
{
    private static CurrentUserService BuildService(ClaimsPrincipal? principal = null, bool hasContext = true)
    {
        var accessor = new Mock<IHttpContextAccessor>();

        if (!hasContext)
        {
            accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        }
        else
        {
            var context = new DefaultHttpContext();
            if (principal is not null)
                context.User = principal;
            accessor.Setup(a => a.HttpContext).Returns(context);
        }

        return new CurrentUserService(accessor.Object);
    }

    private static ClaimsPrincipal PrincipalWithSubClaim(Guid userId) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "TestAuth"));

    private static ClaimsPrincipal AnonymousPrincipal() =>
        new(new ClaimsIdentity());

    [Fact]
    public void TryGetUserId_WhenSubClaimPresent_ReturnsTrueAndCorrectId()
    {
        var expectedId = Guid.NewGuid();
        var service = BuildService(PrincipalWithSubClaim(expectedId));

        var result = service.TryGetUserId(out var userId);

        result.Should().BeTrue();
        userId.Should().Be(expectedId);
    }

    [Fact]
    public void TryGetUserId_WhenNoClaimPresent_ReturnsFalse()
    {
        var service = BuildService(AnonymousPrincipal());

        var result = service.TryGetUserId(out var userId);

        result.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetUserId_WhenNoHttpContext_ReturnsFalse()
    {
        var service = BuildService(hasContext: false);

        var result = service.TryGetUserId(out var userId);

        result.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetUserId_WhenClaimIsNotValidGuid_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")],
            "TestAuth"));
        var service = BuildService(principal);

        var result = service.TryGetUserId(out var userId);

        result.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void UserId_WhenSubClaimPresent_ReturnsCorrectId()
    {
        var expectedId = Guid.NewGuid();
        var service = BuildService(PrincipalWithSubClaim(expectedId));

        service.UserId.Should().Be(expectedId);
    }

    [Fact]
    public void UserId_WhenNoClaimPresent_ThrowsUnauthorizedException()
    {
        var service = BuildService(AnonymousPrincipal());

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedException>()
            .Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public void UserId_WhenNoHttpContext_ThrowsUnauthorizedException()
    {
        var service = BuildService(hasContext: false);

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedException>()
            .Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public void UserId_WhenClaimIsNotValidGuid_ThrowsUnauthorizedException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")],
            "TestAuth"));
        var service = BuildService(principal);

        var act = () => service.UserId;

        act.Should().Throw<UnauthorizedException>()
            .Which.Code.Should().Be("SESSION_INVALID");
    }
}
