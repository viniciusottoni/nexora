using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Domain.Entities.Training;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-237: insere o split map determinístico (v1) dos 5 programas de treino
    /// clássicos (full_body, ab, abc, abcd, abcde) usando SQL bruto idempotente,
    /// no mesmo padrão de <see cref="SeedInitialTrainingPrograms"/> — GUIDs fixos
    /// e <c>ON CONFLICT DO NOTHING</c>, evitando depender de metadata do EF para
    /// data seeding em migrations criadas manualmente.
    ///
    /// `perfect_2` e `system` não recebem split (RN-009) — não há linhas para eles.
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260705050000_SeedInitialTrainingProgramSplits")]
    public partial class SeedInitialTrainingProgramSplits : Migration
    {
        private static readonly DateTime SeedUtcNow = new(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        private const string SplitMapVersion = "v1";

        private static readonly Guid FullBodySplitId = new("d1000001-0000-0000-0000-000000000001");
        private static readonly Guid AbSplitId = new("d1000002-0000-0000-0000-000000000002");
        private static readonly Guid AbcSplitId = new("d1000003-0000-0000-0000-000000000003");
        private static readonly Guid AbcdSplitId = new("d1000004-0000-0000-0000-000000000004");
        private static readonly Guid AbcdeSplitId = new("d1000005-0000-0000-0000-000000000005");

        private sealed record SplitSeed(Guid Id, string ProgramKey, int DayCount);

        private sealed record DaySeed(
            Guid Id,
            Guid TrainingProgramSplitId,
            int DayIndex,
            string DayKey,
            string LabelI18nKey,
            string Role,
            string[] TargetMuscleGroups,
            string[] SecondaryMuscleGroups,
            string[] TargetMovementPatterns,
            bool AllowsCoreFinisher,
            int MinExercises,
            int MaxExercises);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var splits = new[]
            {
                new SplitSeed(FullBodySplitId, TrainingProgramKeys.FullBody, 1),
                new SplitSeed(AbSplitId, TrainingProgramKeys.Ab, 2),
                new SplitSeed(AbcSplitId, TrainingProgramKeys.Abc, 3),
                new SplitSeed(AbcdSplitId, TrainingProgramKeys.Abcd, 4),
                new SplitSeed(AbcdeSplitId, TrainingProgramKeys.Abcde, 5),
            };

            var days = new[]
            {
                // full_body — seção 6.1 da US-237: um único dia lógico (RN-006).
                new DaySeed(
                    new Guid("d2000001-0000-0000-0000-000000000001"), FullBodySplitId, 1, "FB", "programDayFullBody", "full_body",
                    [MuscleGroups.Chest, MuscleGroups.Back, MuscleGroups.Quadriceps, MuscleGroups.Hamstrings, MuscleGroups.Glutes, MuscleGroups.Shoulders, MuscleGroups.Core],
                    [],
                    [MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush, MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull, MovementPatterns.Squat, MovementPatterns.Lunge, MovementPatterns.Hinge, MovementPatterns.CoreFlexion],
                    false, 5, 7),

                // ab — seção 6.2: pernas integradas (push=quadríceps/panturrilha, pull=posterior/glúteo).
                new DaySeed(
                    new Guid("d2000002-0000-0000-0000-000000000002"), AbSplitId, 1, "A", "programDayPush", "push",
                    [MuscleGroups.Chest, MuscleGroups.Shoulders, MuscleGroups.Triceps, MuscleGroups.Quadriceps, MuscleGroups.Calves],
                    [],
                    [MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush, MovementPatterns.Squat, MovementPatterns.Lunge, MovementPatterns.CoreFlexion],
                    true, 4, 6),
                new DaySeed(
                    new Guid("d2000003-0000-0000-0000-000000000003"), AbSplitId, 2, "B", "programDayPull", "pull",
                    [MuscleGroups.Back, MuscleGroups.Biceps, MuscleGroups.RearDelts, MuscleGroups.Hamstrings, MuscleGroups.Glutes],
                    [],
                    [MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull, MovementPatterns.Hinge, MovementPatterns.CoreFlexion],
                    true, 4, 6),

                // abc — seção 6.3: Push / Pull / Legs clássico.
                new DaySeed(
                    new Guid("d2000004-0000-0000-0000-000000000004"), AbcSplitId, 1, "A", "programDayPush", "push",
                    [MuscleGroups.Chest, MuscleGroups.Shoulders, MuscleGroups.Triceps],
                    [],
                    [MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush, MovementPatterns.CoreFlexion],
                    true, 4, 6),
                new DaySeed(
                    new Guid("d2000005-0000-0000-0000-000000000005"), AbcSplitId, 2, "B", "programDayPull", "pull",
                    [MuscleGroups.Back, MuscleGroups.Biceps, MuscleGroups.RearDelts, MuscleGroups.Traps],
                    [],
                    [MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull],
                    false, 4, 6),
                new DaySeed(
                    new Guid("d2000006-0000-0000-0000-000000000006"), AbcSplitId, 3, "C", "programDayLegs", "legs",
                    [MuscleGroups.Quadriceps, MuscleGroups.Hamstrings, MuscleGroups.Glutes, MuscleGroups.Calves, MuscleGroups.Core],
                    [],
                    [MovementPatterns.Squat, MovementPatterns.Hinge, MovementPatterns.Lunge, MovementPatterns.CoreFlexion],
                    true, 4, 6),

                // abcd — seção 6.4: divisão por grupo (intermediário/avançado).
                new DaySeed(
                    new Guid("d2000007-0000-0000-0000-000000000007"), AbcdSplitId, 1, "A", "programDayChest", "chest",
                    [MuscleGroups.Chest, MuscleGroups.Triceps],
                    [],
                    [MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush],
                    false, 3, 5),
                new DaySeed(
                    new Guid("d2000008-0000-0000-0000-000000000008"), AbcdSplitId, 2, "B", "programDayBack", "back",
                    [MuscleGroups.Back, MuscleGroups.Biceps, MuscleGroups.RearDelts],
                    [],
                    [MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull],
                    false, 3, 5),
                new DaySeed(
                    new Guid("d2000009-0000-0000-0000-000000000009"), AbcdSplitId, 3, "C", "programDayLegs", "legs",
                    [MuscleGroups.Quadriceps, MuscleGroups.Hamstrings, MuscleGroups.Glutes, MuscleGroups.Calves],
                    [],
                    [MovementPatterns.Squat, MovementPatterns.Hinge, MovementPatterns.Lunge],
                    false, 3, 5),
                new DaySeed(
                    new Guid("d2000010-0000-0000-0000-000000000010"), AbcdSplitId, 4, "D", "programDayShoulders", "shoulders",
                    [MuscleGroups.Shoulders, MuscleGroups.Traps, MuscleGroups.Core],
                    [],
                    [MovementPatterns.VerticalPush, MovementPatterns.Carry, MovementPatterns.CoreFlexion],
                    true, 3, 5),

                // abcde — seção 6.5: divisão avançada de alto volume (por grupo).
                new DaySeed(
                    new Guid("d2000011-0000-0000-0000-000000000011"), AbcdeSplitId, 1, "A", "programDayChest", "chest",
                    [MuscleGroups.Chest, MuscleGroups.Triceps],
                    [],
                    [MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush],
                    false, 3, 5),
                new DaySeed(
                    new Guid("d2000012-0000-0000-0000-000000000012"), AbcdeSplitId, 2, "B", "programDayBack", "back",
                    [MuscleGroups.Back, MuscleGroups.Traps, MuscleGroups.RearDelts],
                    [],
                    [MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull],
                    false, 3, 5),
                new DaySeed(
                    new Guid("d2000013-0000-0000-0000-000000000013"), AbcdeSplitId, 3, "C", "programDayLegs", "legs",
                    [MuscleGroups.Quadriceps, MuscleGroups.Hamstrings, MuscleGroups.Glutes, MuscleGroups.Calves],
                    [],
                    [MovementPatterns.Squat, MovementPatterns.Hinge, MovementPatterns.Lunge],
                    false, 3, 5),
                new DaySeed(
                    new Guid("d2000014-0000-0000-0000-000000000014"), AbcdeSplitId, 4, "D", "programDayShoulders", "shoulders",
                    [MuscleGroups.Shoulders, MuscleGroups.Traps],
                    [],
                    [MovementPatterns.VerticalPush],
                    false, 3, 5),
                new DaySeed(
                    new Guid("d2000015-0000-0000-0000-000000000015"), AbcdeSplitId, 5, "E", "programDayArms", "arms",
                    [MuscleGroups.Biceps, MuscleGroups.Triceps, MuscleGroups.Forearms, MuscleGroups.Core],
                    [],
                    [MovementPatterns.HorizontalPull, MovementPatterns.VerticalPull, MovementPatterns.HorizontalPush, MovementPatterns.VerticalPush, MovementPatterns.Carry, MovementPatterns.CoreFlexion],
                    true, 3, 5),
            };

            var splitsSql = $@"
INSERT INTO ""training_program_splits"" (
    ""Id"", ""ProgramKey"", ""SplitMapVersion"", ""DayCount"", ""IsActive"",
    ""CreatedAtUtc"", ""UpdatedAtUtc"", ""DeletedAtUtc"", ""CreatedByUserId"", ""UpdatedByUserId"", ""IsDeleted""
)
VALUES
{string.Join(",\n", splits.Select(BuildSplitValuesClause))}
ON CONFLICT (""ProgramKey"") DO NOTHING;
";
            migrationBuilder.Sql(splitsSql);

            var daysSql = $@"
INSERT INTO ""training_split_days"" (
    ""Id"", ""TrainingProgramSplitId"", ""DayIndex"", ""DayKey"", ""LabelI18nKey"", ""Role"",
    ""TargetMuscleGroups"", ""SecondaryMuscleGroups"", ""TargetMovementPatterns"",
    ""AllowsCoreFinisher"", ""MinExercises"", ""MaxExercises"",
    ""CreatedAtUtc"", ""UpdatedAtUtc"", ""DeletedAtUtc"", ""CreatedByUserId"", ""UpdatedByUserId"", ""IsDeleted""
)
VALUES
{string.Join(",\n", days.Select(BuildDayValuesClause))}
ON CONFLICT (""TrainingProgramSplitId"", ""DayIndex"") DO NOTHING;
";
            migrationBuilder.Sql(daysSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var splitIds = new[] { FullBodySplitId, AbSplitId, AbcSplitId, AbcdSplitId, AbcdeSplitId };

            // A FK de training_split_days -> training_program_splits está em cascade,
            // então apagar os splits já remove os dias correspondentes.
            migrationBuilder.Sql($@"
DELETE FROM ""training_program_splits""
WHERE ""Id"" IN ({string.Join(", ", splitIds.Select(SqlGuid))});
");
        }

        private static string BuildSplitValuesClause(SplitSeed split) =>
            $"({SqlGuid(split.Id)}, {SqlString(split.ProgramKey)}, {SqlString(SplitMapVersion)}, {split.DayCount}, " +
            $"{SqlBool(true)}, {SqlTimestamp(SeedUtcNow)}, NULL, NULL, NULL, NULL, {SqlBool(false)})";

        private static string BuildDayValuesClause(DaySeed day) =>
            $"({SqlGuid(day.Id)}, {SqlGuid(day.TrainingProgramSplitId)}, {day.DayIndex}, {SqlString(day.DayKey)}, " +
            $"{SqlString(day.LabelI18nKey)}, {SqlString(day.Role)}, " +
            $"{SqlStringArray(day.TargetMuscleGroups)}, {SqlStringArray(day.SecondaryMuscleGroups)}, {SqlStringArray(day.TargetMovementPatterns)}, " +
            $"{SqlBool(day.AllowsCoreFinisher)}, {day.MinExercises}, {day.MaxExercises}, " +
            $"{SqlTimestamp(SeedUtcNow)}, NULL, NULL, NULL, NULL, {SqlBool(false)})";

        private static string SqlGuid(Guid value) => $"'{value:D}'";

        private static string SqlString(string value) => $"'{value.Replace("'", "''")}'";

        private static string SqlBool(bool value) => value ? "TRUE" : "FALSE";

        private static string SqlTimestamp(DateTime value) => $"TIMESTAMPTZ '{value:O}'";

        private static string SqlStringArray(IReadOnlyList<string> values) =>
            values.Count == 0
                ? "ARRAY[]::text[]"
                : $"ARRAY[{string.Join(", ", values.Select(SqlString))}]::text[]";
    }
}
