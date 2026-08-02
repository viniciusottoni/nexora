using Awaken.Application.Auth.Commands.Register;
using FluentAssertions;

namespace Awaken.UnitTests.Auth;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    [Fact]
    public void ValidatesSuccessfullyWithValidData()
    {
        var command = new RegisterUserCommand("hunter@awaken.app", "Str0ngPass!", "Hunter", "pt-BR");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FailsWhenDisplayNameIsEmptyOrMissing(string? displayName)
    {
        var command = new RegisterUserCommand("hunter@awaken.app", "Str0ngPass!", displayName, "pt-BR");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.DisplayName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void FailsWhenEmailIsEmptyOrInvalid(string email)
    {
        var command = new RegisterUserCommand(email, "Str0ngPass!", "Hunter");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void FailsWhenPasswordIsEmptyOrTooShort(string password)
    {
        var command = new RegisterUserCommand("hunter@awaken.app", password, "Hunter");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.Password));
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    public void ValidatesSuccessfullyWithSupportedLanguages(string language)
    {
        var command = new RegisterUserCommand("hunter@awaken.app", "Str0ngPass!", "Hunter", language);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FailsWhenLanguageIsNotSupported()
    {
        var command = new RegisterUserCommand("hunter@awaken.app", "Str0ngPass!", "Hunter", "de");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.Language));
    }
}
