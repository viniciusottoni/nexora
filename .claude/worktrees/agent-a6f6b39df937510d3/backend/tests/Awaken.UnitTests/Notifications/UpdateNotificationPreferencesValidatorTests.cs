using Awaken.Application.Notifications.Commands.UpdatePreferences;
using FluentAssertions;

namespace Awaken.UnitTests.Notifications;

public class UpdateNotificationPreferencesValidatorTests
{
    private readonly UpdateNotificationPreferencesValidator _validator = new();

    [Fact]
    public void ValidatesSuccessfullyWithPushEnabledAndToken()
    {
        var command = new UpdateNotificationPreferencesCommand(true, "fcm-token-xyz", "granted");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatesSuccessfullyWithPushDisabledAndNoToken()
    {
        var command = new UpdateNotificationPreferencesCommand(false, null, "denied");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatesSuccessfullyWithPushEnabledAndNullToken()
    {
        // RN-003: token pendente — null token with pushEnabled=true is valid
        var command = new UpdateNotificationPreferencesCommand(true, null, "granted");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FailsWhenPushTokenExceedsMaxLength()
    {
        var longToken = new string('x', 513);
        var command = new UpdateNotificationPreferencesCommand(true, longToken, "granted");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNotificationPreferencesCommand.PushToken));
    }

    [Fact]
    public void FailsWhenPushEnabledButTokenIsEmptyString()
    {
        var command = new UpdateNotificationPreferencesCommand(true, "", "granted");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNotificationPreferencesCommand.PushToken));
    }

    [Theory]
    [InlineData("not_requested")]
    [InlineData("requesting")]
    [InlineData("granted")]
    [InlineData("denied")]
    [InlineData("token_registered")]
    [InlineData("sync_error")]
    [InlineData(null)]
    public void ValidatesSuccessfullyWithValidOrNullPermissionStatus(string? status)
    {
        var command = new UpdateNotificationPreferencesCommand(false, null, status);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("unknown_status")]
    [InlineData("GRANTED")]
    [InlineData("enabled")]
    public void FailsWithInvalidPermissionStatus(string status)
    {
        var command = new UpdateNotificationPreferencesCommand(false, null, status);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNotificationPreferencesCommand.PermissionStatus));
    }
}
