using Awaken.Domain.Entities.Quests;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

// US-238: registra o dia do programa resolvido pela rotação cíclica (RN-009)
// na própria quest gerada, para auditoria e para servir de base à próxima rotação.
public class QuestTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);

    private static Quest BuildQuest() => Quest.Create(
        Guid.NewGuid(), UtcNow.Date, "pt-BR", Guid.NewGuid().ToString());

    [Fact]
    public void AssignResolvedProgramDay_SetsAllFourFieldsAndUpdatedAtUtc()
    {
        var quest = BuildQuest();

        quest.AssignResolvedProgramDay("abc", "B", 2, "v1", UtcNow);

        quest.ResolvedProgramKey.Should().Be("abc");
        quest.ResolvedDayKey.Should().Be("B");
        quest.ResolvedDayIndex.Should().Be(2);
        quest.SplitMapVersion.Should().Be("v1");
        quest.TrainingType.Should().Be("program");
        quest.ProgramId.Should().Be("abc");
        quest.UpdatedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public void ResolvedProgramDayFields_AreNull_BeforeAssignment()
    {
        var quest = BuildQuest();

        quest.ResolvedProgramKey.Should().BeNull();
        quest.ResolvedDayKey.Should().BeNull();
        quest.ResolvedDayIndex.Should().BeNull();
        quest.SplitMapVersion.Should().BeNull();
    }

    // US-240: registra o snapshot do DailyWorkoutBlueprint usado nesta geração (RN-008/US-049).
    [Fact]
    public void AssignDailyWorkoutBlueprint_SetsJsonAndUpdatedAtUtc()
    {
        var quest = BuildQuest();

        quest.AssignDailyWorkoutBlueprint("{\"programKey\":\"abc\"}", UtcNow);

        quest.DailyWorkoutBlueprintJson.Should().Be("{\"programKey\":\"abc\"}");
        quest.UpdatedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public void DailyWorkoutBlueprintJson_IsNull_BeforeAssignment()
    {
        var quest = BuildQuest();

        quest.DailyWorkoutBlueprintJson.Should().BeNull();
    }
}
