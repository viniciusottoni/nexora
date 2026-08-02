using Awaken.Application.Auth.Commands.Login;
using FluentAssertions;

namespace Awaken.UnitTests.Auth;

public class LoginUserValidatorTests
{
    private readonly LoginUserValidator _validator = new();

    [Fact]
    public void ValidatesSuccessfullyWithValidData()
    {
        var command = new LoginUserCommand("hunter@awaken.app", "Str0ngPass!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void FailsWhenEmailIsEmptyOrInvalid(string email)
    {
        var command = new LoginUserCommand(email, "Str0ngPass!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginUserCommand.Email));
    }

    [Fact]
    public void FailsWhenPasswordIsEmpty()
    {
        var command = new LoginUserCommand("hunter@awaken.app", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginUserCommand.Password));
    }
}
