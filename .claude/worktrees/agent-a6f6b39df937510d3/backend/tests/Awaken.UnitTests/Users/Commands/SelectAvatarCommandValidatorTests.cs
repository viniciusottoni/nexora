using Awaken.Application.Users.Commands.SelectAvatar;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Users.Commands;

public class SelectAvatarCommandValidatorTests
{
    private readonly SelectAvatarCommandValidator _validator = new();

    [Fact]
    public void Rejects_EmptyAvatarKey()
    {
        var result = _validator.TestValidate(new SelectAvatarCommand(string.Empty));
        result.ShouldHaveValidationErrorFor(x => x.AvatarKey);
    }

    [Fact]
    public void Rejects_AvatarKeyLongerThanMaxLength()
    {
        var result = _validator.TestValidate(new SelectAvatarCommand(new string('a', 65)));
        result.ShouldHaveValidationErrorFor(x => x.AvatarKey);
    }

    [Fact]
    public void Accepts_ValidAvatarKey()
    {
        var result = _validator.TestValidate(new SelectAvatarCommand("avatar_male_1"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
