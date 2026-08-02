using Awaken.Domain.Entities.Training;
using FluentAssertions;

namespace Awaken.UnitTests.Training;

// US-239: MuscleRecoveryState acumula volume semanal por grupo muscular e
// reseta o acumulado quando a virada de semana ocorre (RN-002/CA-004).
public class MuscleRecoveryStateTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // Domingo 2026-07-05 00:00 UTC — âncora de semana.
    private static readonly DateTime SundayThisWeek = new(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RegisterSession_AccumulatesWeeklySets_WithinTheSameWeek()
    {
        var state = MuscleRecoveryState.CreateInitial(UserId, MuscleGroups.Quadriceps, SundayThisWeek);

        state.RegisterSession(SundayThisWeek, "heavy", ["squat_family"], setsPerformed: 4);
        state.RegisterSession(SundayThisWeek.AddDays(2), "moderate", ["lunge_family"], setsPerformed: 3);

        state.WeeklySetsAccumulated.Should().Be(7);
        state.LastIntensity.Should().Be("moderate");
        state.LastMovementFamilies.Should().BeEquivalentTo(["lunge_family"]);
    }

    [Fact]
    public void RegisterSession_ResetsWeeklySets_WhenWeekAnchorChanges()
    {
        var state = MuscleRecoveryState.CreateInitial(UserId, MuscleGroups.Quadriceps, SundayThisWeek);
        state.RegisterSession(SundayThisWeek, "heavy", ["squat_family"], setsPerformed: 4);
        var anchorBefore = state.WeekAnchorDateUtc;

        var nextWeek = SundayThisWeek.AddDays(7);
        state.RegisterSession(nextWeek, "light", ["hinge_family"], setsPerformed: 2);

        state.WeekAnchorDateUtc.Should().NotBe(anchorBefore);
        state.WeekAnchorDateUtc.Should().Be(nextWeek.Date);
        state.WeeklySetsAccumulated.Should().Be(2); // não acumula com a semana anterior
    }
}
