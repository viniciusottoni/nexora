using Awaken.Domain.Entities.Training;
using FluentAssertions;

namespace Awaken.UnitTests.Training;

// US-237: MovementPatterns é o enum interno (US-145/US-236) de padrões de
// movimento usado para validar o split map — padrão fora desta lista bloqueia
// o seed (RN-005).
public class MovementPatternsTests
{
    [Theory]
    [InlineData(MovementPatterns.Squat)]
    [InlineData(MovementPatterns.Hinge)]
    [InlineData(MovementPatterns.HorizontalPush)]
    [InlineData(MovementPatterns.VerticalPush)]
    [InlineData(MovementPatterns.HorizontalPull)]
    [InlineData(MovementPatterns.VerticalPull)]
    [InlineData(MovementPatterns.Lunge)]
    [InlineData(MovementPatterns.Carry)]
    [InlineData(MovementPatterns.CoreFlexion)]
    [InlineData(MovementPatterns.CoreAntiExtension)]
    [InlineData(MovementPatterns.CoreAntiRotation)]
    [InlineData(MovementPatterns.Locomotion)]
    [InlineData(MovementPatterns.Jump)]
    [InlineData(MovementPatterns.Balance)]
    [InlineData(MovementPatterns.Mobility)]
    public void IsValid_ReturnsTrue_ForKnownMovementPatterns(string pattern)
    {
        MovementPatterns.IsValid(pattern).Should().BeTrue();
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForUnknownMovementPattern()
    {
        MovementPatterns.IsValid("bogus_pattern").Should().BeFalse();
    }
}
