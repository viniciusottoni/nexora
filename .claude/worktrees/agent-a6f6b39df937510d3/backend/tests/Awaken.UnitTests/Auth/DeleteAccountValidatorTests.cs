using Awaken.Application.Auth.Commands.DeleteAccount;
using FluentAssertions;

namespace Awaken.UnitTests.Auth;

public class DeleteAccountValidatorTests
{
    private readonly DeleteAccountValidator _validator = new();

    [Fact]
    public async Task ValidateSucceeds()
    {
        var result = await _validator.ValidateAsync(new DeleteAccountCommand());

        result.IsValid.Should().BeTrue();
    }
}
