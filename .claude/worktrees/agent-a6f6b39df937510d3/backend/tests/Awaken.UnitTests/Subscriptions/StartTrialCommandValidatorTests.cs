using Awaken.Application.Subscriptions.Commands.StartTrial;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Subscriptions;

public class StartTrialCommandValidatorTests
{
    private readonly StartTrialCommandValidator _validator = new();

    [Fact]
    public void ValidatorPassesForEmptyCommand()
    {
        var result = _validator.TestValidate(new StartTrialCommand());
        result.IsValid.Should().BeTrue();
    }
}
