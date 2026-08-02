using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// US-208: indexes de performance para queries frequentes em jobs e leituras do hunter.
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Quest: busca do job de penalidade (Type + QuestDateUtc + PenaltyCheckedAtUtc IS NULL)
            migrationBuilder.CreateIndex(
                name: "IX_quests_Type_QuestDateUtc_Status",
                table: "quests",
                columns: new[] { "Type", "QuestDateUtc", "Status" });

            // Quest: partial index para dailies ainda nao verificadas pelo job de penalidade.
            // Reduz drasticamente o scan quando a maioria das quests ja foi verificada.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_quests_PenaltyCheckedAtUtc_null\" " +
                "ON quests (\"Id\") " +
                "WHERE \"PenaltyCheckedAtUtc\" IS NULL;",
                suppressTransaction: true);

            // Notification preferences: partial index para usuarios com push ativo.
            // O job de lembrete so precisa desses registros.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_notification_preferences_PushEnabled_true\" " +
                "ON notification_preferences (\"Id\") " +
                "WHERE \"PushEnabled\" = true;",
                suppressTransaction: true);

            // Notification logs: filtragem por tipo dentro do historico do usuario (limite diario).
            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_UserId_NotificationType_AttemptedAtUtc",
                table: "notification_logs",
                columns: new[] { "UserId", "NotificationType", "AttemptedAtUtc" });

            // Subscriptions: verifica status de acesso por usuario (job de penalidade e lembretes).
            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId_Status",
                table: "subscriptions",
                columns: new[] { "UserId", "Status" });

            // Quest logs: cursor pagination por usuario ordenado por data de conclusao.
            migrationBuilder.CreateIndex(
                name: "IX_quest_logs_UserId_CompletedAtUtc",
                table: "quest_logs",
                columns: new[] { "UserId", "CompletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quests_Type_QuestDateUtc_Status",
                table: "quests");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_quests_PenaltyCheckedAtUtc_null\";");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_notification_preferences_PushEnabled_true\";");

            migrationBuilder.DropIndex(
                name: "IX_notification_logs_UserId_NotificationType_AttemptedAtUtc",
                table: "notification_logs");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_UserId_Status",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_quest_logs_UserId_CompletedAtUtc",
                table: "quest_logs");
        }
    }
}
