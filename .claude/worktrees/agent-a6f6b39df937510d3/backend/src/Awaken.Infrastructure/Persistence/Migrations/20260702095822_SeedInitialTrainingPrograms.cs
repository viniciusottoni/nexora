using System;
using Awaken.Domain.Entities.Training;
using Awaken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-231: insere o catalogo inicial de programas de treino usando SQL bruto
    /// idempotente. Isso evita a dependencia de metadata do EF para data seeding
    /// em migrations criadas manualmente.
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AwakenDbContext))]
    [Migration("20260702095822_SeedInitialTrainingPrograms")]
    public partial class SeedInitialTrainingPrograms : Migration
    {
        private static readonly DateTime SeedUtcNow = new(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc);

        private static readonly Guid FullBodyId = new("f1000001-0000-0000-0000-000000000001");
        private static readonly Guid AbId = new("f1000002-0000-0000-0000-000000000002");
        private static readonly Guid AbcId = new("f1000003-0000-0000-0000-000000000003");
        private static readonly Guid AbcdId = new("f1000004-0000-0000-0000-000000000004");
        private static readonly Guid AbcdeId = new("f1000005-0000-0000-0000-000000000005");
        private static readonly Guid Perfect2Id = new("f1000006-0000-0000-0000-000000000006");
        private static readonly Guid SystemId = new("f1000007-0000-0000-0000-000000000007");

        private sealed record TrainingProgramSeed(
            Guid Id,
            string Key,
            string Name,
            string? Description,
            string? Category,
            string MinimumRank,
            int? SplitDays,
            bool IsActive,
            DateTime CreatedAtUtc,
            DateTime? UpdatedAtUtc,
            DateTime? DeletedAtUtc,
            Guid? CreatedByUserId,
            Guid? UpdatedByUserId,
            bool IsDeleted);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var programs = new[]
            {
                new TrainingProgramSeed(FullBodyId, TrainingProgramKeys.FullBody, "Full Body",
                    "Corpo inteiro em cada sessao.",
                    "Sedentario", "E+", 1, true, SeedUtcNow, null, null, null, null, false),
                new TrainingProgramSeed(AbId, TrainingProgramKeys.Ab, "AB",
                    "Push + Pull com pernas integradas.",
                    "Sedentario, Iniciante", "D+", 2, true, SeedUtcNow, null, null, null, null, false),
                new TrainingProgramSeed(AbcId, TrainingProgramKeys.Abc, "ABC",
                    "Classico das academias com maior divisao muscular.",
                    "Intermediario", "C+", 3, true, SeedUtcNow, null, null, null, null, false),
                new TrainingProgramSeed(AbcdId, TrainingProgramKeys.Abcd, "ABCD",
                    "Divisao intermediaria/avancada para maior foco por grupo.",
                    "Intermediario, Avancado", "C+", 4, true, SeedUtcNow, null, null, null, null, false),
                new TrainingProgramSeed(AbcdeId, TrainingProgramKeys.Abcde, "ABCDE",
                    "Divisao avancada com alto volume semanal.",
                    "Avancado", "B+", 5, true, SeedUtcNow, null, null, null, null, false),
                new TrainingProgramSeed(Perfect2Id, TrainingProgramKeys.Perfect2, "Perfect 2",
                    "Dois exercicios ideais por grupo muscular, com execucao intensa.",
                    "Intermediario, Avancado", "C+", 2, true, SeedUtcNow, null, null, null, null, false),
                new TrainingProgramSeed(SystemId, TrainingProgramKeys.System, "System",
                    "Protocolo especial estilo desafio, acessivel a qualquer rank, com adaptacao por nivel.",
                    "Qualquer um", "E+", null, true, SeedUtcNow, null, null, null, null, false),
            };

            var sql = $@"
INSERT INTO ""training_programs"" (
    ""Id"", ""Key"", ""Name"", ""Description"", ""Category"", ""MinimumRank"", ""SplitDays"",
    ""IsActive"", ""CreatedAtUtc"", ""UpdatedAtUtc"", ""DeletedAtUtc"",
    ""CreatedByUserId"", ""UpdatedByUserId"", ""IsDeleted""
)
VALUES
{string.Join(",\n", programs.Select(BuildValuesClause))}
ON CONFLICT (""Key"") DO NOTHING;
";

            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var ids = new[]
            {
                FullBodyId,
                AbId,
                AbcId,
                AbcdId,
                AbcdeId,
                Perfect2Id,
                SystemId,
            };

            migrationBuilder.Sql($@"
DELETE FROM ""training_programs""
WHERE ""Id"" IN ({string.Join(", ", ids.Select(SqlGuid))});
");
        }

        private static string BuildValuesClause(TrainingProgramSeed program) =>
            $"({SqlGuid(program.Id)}, {SqlString(program.Key)}, {SqlString(program.Name)}, {SqlString(program.Description)}, " +
            $"{SqlString(program.Category)}, {SqlString(program.MinimumRank)}, {SqlNullableInt(program.SplitDays)}, " +
            $"{SqlBool(program.IsActive)}, {SqlTimestamp(program.CreatedAtUtc)}, {SqlNullableTimestamp(program.UpdatedAtUtc)}, " +
            $"{SqlNullableTimestamp(program.DeletedAtUtc)}, {SqlNullableGuid(program.CreatedByUserId)}, " +
            $"{SqlNullableGuid(program.UpdatedByUserId)}, {SqlBool(program.IsDeleted)})";

        private static string SqlGuid(Guid value) => $"'{value:D}'";

        private static string SqlNullableGuid(Guid? value) => value.HasValue ? SqlGuid(value.Value) : "NULL";

        private static string SqlString(string? value) =>
            value is null ? "NULL" : $"'{value.Replace("'", "''")}'";

        private static string SqlNullableInt(int? value) => value.HasValue ? value.Value.ToString() : "NULL";

        private static string SqlBool(bool value) => value ? "TRUE" : "FALSE";

        private static string SqlTimestamp(DateTime value) => $"TIMESTAMPTZ '{value:O}'";

        private static string SqlNullableTimestamp(DateTime? value) =>
            value.HasValue ? SqlTimestamp(value.Value) : "NULL";
    }
}
