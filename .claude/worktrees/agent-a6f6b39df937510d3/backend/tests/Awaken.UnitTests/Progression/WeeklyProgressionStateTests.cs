using Awaken.Domain.Entities.Progression;
using FluentAssertions;
using Xunit;

namespace Awaken.UnitTests.Progression;

public class WeeklyProgressionStateTests
{
    [Fact]
    public void ApplyWeeklyDecision_AdvancesMesocycleOnlyOnNewWeek()
    {
        var state = WeeklyProgressionState.CreateInitial(Guid.NewGuid(), new DateOnly(2026, 7, 6), null, DateTime.UtcNow);
        state.ApplyWeeklyDecision(new DateOnly(2026, 7, 6), isNewWeek: false, "hold", null, 0, 0, 0, 0, 0, false, null, DateTime.UtcNow);
        state.MesocycleWeekIndex.Should().Be(1);

        state.ApplyWeeklyDecision(new DateOnly(2026, 7, 13), isNewWeek: true, "progress", "add_set", 1, 0, 0, 1, 0, false, null, DateTime.UtcNow);
        state.MesocycleWeekIndex.Should().Be(2);
    }

    [Fact]
    public void ApplyWeeklyDecision_ResetsMesocycleToOneWhenDeloadApplied()
    {
        var state = WeeklyProgressionState.CreateInitial(Guid.NewGuid(), new DateOnly(2026, 7, 6), null, DateTime.UtcNow);
        for (var i = 0; i < 5; i++)
            state.ApplyWeeklyDecision(new DateOnly(2026, 7, 6).AddDays(7 * (i + 1)), true, "progress", "add_set", i, 0, 0, i + 1, 0, false, null, DateTime.UtcNow);

        state.ApplyWeeklyDecision(new DateOnly(2026, 8, 10), isNewWeek: true, "deload", null, 0, -1, 0, 0, 0, deloadApplied: true, null, DateTime.UtcNow);
        state.MesocycleWeekIndex.Should().Be(1);
        state.DeloadDue.Should().BeTrue();
    }

    [Fact]
    public void ApplyWeeklyDecision_StoresProfileSnapshotHash()
    {
        var state = WeeklyProgressionState.CreateInitial(Guid.NewGuid(), new DateOnly(2026, 7, 6), "hash-1", DateTime.UtcNow);
        state.ProfileSnapshotHash.Should().Be("hash-1");

        state.ApplyWeeklyDecision(new DateOnly(2026, 7, 13), true, "hold", null, 0, 0, 0, 0, 0, false, "hash-2", DateTime.UtcNow);
        state.ProfileSnapshotHash.Should().Be("hash-2");
    }
}
