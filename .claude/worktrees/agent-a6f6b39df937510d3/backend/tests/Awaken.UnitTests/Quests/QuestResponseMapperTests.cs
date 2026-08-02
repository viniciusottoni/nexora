using Awaken.Application.Quests.Common;
using Awaken.Domain.Entities.Quests;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

// US-240: WorkoutGeneratorService grava resolvedProgramKey/resolvedDayKey/
// dayLabelI18nKey/splitMapVersion/hasMuscleGroupInRecovery dentro do próprio
// WorkoutJson (5.2); o mapper precisa repassar esses campos para o WorkoutDto
// consumido pelo Flutter (5.3).
public class QuestResponseMapperTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);

    private static Quest BuildQuestWithWorkout(string workoutJson)
    {
        var quest = Quest.Create(Guid.NewGuid(), UtcNow.Date, "pt-BR", Guid.NewGuid().ToString());
        quest.AssignWorkout(workoutJson, UtcNow);
        return quest;
    }

    [Fact]
    public void ToResponse_MapsResolvedDayFields_WhenPresentInWorkoutJson()
    {
        const string workoutJson = """
        {
          "title": "Treino",
          "description": "Pernas",
          "durationMinutes": 30,
          "resolvedProgramKey": "abc",
          "resolvedDayKey": "C",
          "dayLabelI18nKey": "programDayLegs",
          "splitMapVersion": "v1",
          "hasMuscleGroupInRecovery": true,
          "exercises": []
        }
        """;
        var quest = BuildQuestWithWorkout(workoutJson);

        var response = QuestResponseMapper.ToResponse(quest);

        response.Workout.Should().NotBeNull();
        response.Workout!.ResolvedProgramKey.Should().Be("abc");
        response.Workout.ResolvedDayKey.Should().Be("C");
        response.Workout.DayLabelI18nKey.Should().Be("programDayLegs");
        response.Workout.SplitMapVersion.Should().Be("v1");
        response.Workout.HasMuscleGroupInRecovery.Should().BeTrue();
    }

    [Fact]
    public void ToResponse_LeavesResolvedDayFieldsNull_WhenAbsentFromWorkoutJson()
    {
        const string workoutJson = """
        {
          "title": "Treino",
          "description": "Fallback",
          "durationMinutes": 30,
          "exercises": []
        }
        """;
        var quest = BuildQuestWithWorkout(workoutJson);

        var response = QuestResponseMapper.ToResponse(quest);

        response.Workout.Should().NotBeNull();
        response.Workout!.ResolvedProgramKey.Should().BeNull();
        response.Workout.ResolvedDayKey.Should().BeNull();
        response.Workout.HasMuscleGroupInRecovery.Should().BeFalse();
    }

    // US-041 (R2.1): WorkoutGeneratorService grava instructions/tips dentro de cada
    // exercicio do WorkoutJson; o mapper precisa repassar os dois campos para o
    // ExerciseDto consumido pelo Flutter (tela de pre-quest) - hoje eles se perdem.
    [Fact]
    public void ToResponse_MapsInstructionsAndTips_WhenPresentInWorkoutJson()
    {
        const string workoutJson = """
        {
          "title": "Treino",
          "description": "Pernas",
          "durationMinutes": 30,
          "exercises": [
            {
              "id": "ex-1",
              "name": "Agachamento",
              "description": "Agachamento livre",
              "sets": 3,
              "repsMin": 8,
              "instructions": ["Pes na largura dos ombros", "Desca controlado"],
              "tips": ["Mantenha o core ativado"]
            }
          ]
        }
        """;
        var quest = BuildQuestWithWorkout(workoutJson);

        var response = QuestResponseMapper.ToResponse(quest);

        response.Workout.Should().NotBeNull();
        var exercise = response.Workout!.Exercises.Single();
        exercise.Instructions.Should().BeEquivalentTo(["Pes na largura dos ombros", "Desca controlado"]);
        exercise.Tips.Should().BeEquivalentTo(["Mantenha o core ativado"]);
    }

    [Fact]
    public void ToResponse_LeavesInstructionsAndTipsEmpty_WhenAbsentFromWorkoutJson()
    {
        const string workoutJson = """
        {
          "title": "Treino",
          "description": "Fallback",
          "durationMinutes": 30,
          "exercises": [
            { "name": "Prancha", "description": "Prancha isometrica", "sets": 3, "repsMin": 1 }
          ]
        }
        """;
        var quest = BuildQuestWithWorkout(workoutJson);

        var response = QuestResponseMapper.ToResponse(quest);

        var exercise = response.Workout!.Exercises.Single();
        exercise.Instructions.Should().BeEmpty();
        exercise.Tips.Should().BeEmpty();
    }
}
