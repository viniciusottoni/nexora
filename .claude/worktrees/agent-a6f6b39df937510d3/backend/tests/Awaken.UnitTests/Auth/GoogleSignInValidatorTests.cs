using Awaken.Application.Auth.Commands.GoogleSignIn;
using FluentAssertions;

namespace Awaken.UnitTests.Auth;

public class GoogleSignInValidatorTests
{
    private readonly GoogleSignInValidator _validator = new();

    [Fact]
    public void ValidatesSuccessfullyWithValidData()
    {
        var command = new GoogleSignInCommand("google", "valid-id-token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FailsWhenProviderCredentialIsEmpty()
    {
        var command = new GoogleSignInCommand("google", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GoogleSignInCommand.ProviderCredential));
    }

    [Theory]
    [InlineData("")]
    [InlineData("facebook")]
    [InlineData("apple")]
    public void FailsWhenProviderIsNotGoogle(string provider)
    {
        var command = new GoogleSignInCommand(provider, "valid-id-token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GoogleSignInCommand.Provider));
    }
}
