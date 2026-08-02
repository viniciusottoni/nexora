using Awaken.Application.Notifications.Commands.UpdateReminderTime;
using FluentAssertions;

namespace Awaken.UnitTests.Notifications;

public class UpdateReminderTimeValidatorTests
{
    private readonly UpdateReminderTimeValidator _validator = new();

    [Fact]
    public void ValidatesSuccessfullyWithValidTimezone()
    {
        var command = new UpdateReminderTimeCommand(new TimeOnly(19, 30), "America/Recife");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("America/Sao_Paulo")]
    [InlineData("Europe/London")]
    [InlineData("UTC")]
    [InlineData("Asia/Tokyo")]
    public void ValidatesSuccessfullyWithVariousValidTimezones(string timezone)
    {
        var command = new UpdateReminderTimeCommand(new TimeOnly(8, 0), timezone);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FailsWhenTimezoneIsEmpty()
    {
        var command = new UpdateReminderTimeCommand(new TimeOnly(19, 30), "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateReminderTimeCommand.Timezone));
    }

    [Fact]
    public void FailsWhenTimezoneExceedsMaxLength()
    {
        var longTimezone = new string('x', 101);
        var command = new UpdateReminderTimeCommand(new TimeOnly(19, 30), longTimezone);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateReminderTimeCommand.Timezone));
    }

    [Fact]
    public void FailsWhenTimezoneHasLeadingWhitespace()
    {
        var command = new UpdateReminderTimeCommand(new TimeOnly(19, 30), " America/Recife");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateReminderTimeCommand.Timezone));
    }

    [Fact]
    public void FailsWhenTimezoneHasTrailingWhitespace()
    {
        var command = new UpdateReminderTimeCommand(new TimeOnly(19, 30), "America/Recife ");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateReminderTimeCommand.Timezone));
    }

    [Fact]
    public void ValidatesSuccessfullyWithMaxLengthTimezone()
    {
        var exactly100Chars = new string('x', 100);
        var command = new UpdateReminderTimeCommand(new TimeOnly(19, 30), exactly100Chars);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
