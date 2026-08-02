using Awaken.Infrastructure.Services;
using FluentAssertions;

namespace Awaken.UnitTests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashProducesValueDifferentFromPlainPassword()
    {
        var hash = _hasher.Hash("Str0ngPass!");

        hash.Should().NotBe("Str0ngPass!");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void VerifyReturnsTrueForMatchingPassword()
    {
        var hash = _hasher.Hash("Str0ngPass!");

        _hasher.Verify("Str0ngPass!", hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyReturnsFalseForNonMatchingPassword()
    {
        var hash = _hasher.Hash("Str0ngPass!");

        _hasher.Verify("WrongPass!", hash).Should().BeFalse();
    }
}
