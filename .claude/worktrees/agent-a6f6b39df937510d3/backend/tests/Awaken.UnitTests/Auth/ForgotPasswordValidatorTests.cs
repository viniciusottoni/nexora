using Awaken.Application.Auth.Commands.ForgotPassword;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Auth;

public class ForgotPasswordValidatorTests
{
    private readonly ForgotPasswordValidator _sut = new();

    [Fact]
    public void ValidEmailPassesValidation()
    {
        var result = _sut.TestValidate(new ForgotPasswordCommand("hunter@awaken.app"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyEmailFailsValidation(string? email)
    {
        var result = _sut.TestValidate(new ForgotPasswordCommand(email!));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void InvalidEmailFormatFailsValidation(string email)
    {
        var result = _sut.TestValidate(new ForgotPasswordCommand(email));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailExceeding256CharsFailsValidation()
    {
        var longEmail = new string('a', 252) + "@b.com"; // 258 chars total
        var result = _sut.TestValidate(new ForgotPasswordCommand(longEmail));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
