using Awaken.Application.Quests.Commands.CompleteQuest;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Awaken.UnitTests.Quests;

public class CompleteQuestCommandValidatorTests
{
    private readonly CompleteQuestCommandValidator _validator = new();

    [Fact]
    public void Validate_FailsWhenQuestIdIsEmpty()
    {
        var result = _validator.TestValidate(new CompleteQuestCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.QuestId);
    }

    [Theory]
    [InlineData("too_easy")]
    [InlineData("just_right")]
    [InlineData("too_hard")]
    [InlineData(null)]
    public void Validate_AcceptsValidPerceivedFeelingValues(string? feeling)
    {
        var result = _validator.TestValidate(new CompleteQuestCommand(Guid.NewGuid(), feeling));

        result.ShouldNotHaveValidationErrorFor(x => x.PerceivedFeeling);
    }

    [Fact]
    public void Validate_FailsWhenPerceivedFeelingIsNotAKnownValue()
    {
        var result = _validator.TestValidate(new CompleteQuestCommand(Guid.NewGuid(), "kinda_ok"));

        result.ShouldHaveValidationErrorFor(x => x.PerceivedFeeling);
    }
}
