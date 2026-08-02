using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// US-026 (Solicitar a conta): adiciona <c>split_mode</c>/<c>split_people</c> em
    /// <c>table_session</c> — preferência de divisão registrada na solicitação da conta (ver
    /// <c>TableSession.RequestBill</c>/<c>TableSession.SplitMode</c>). A US-027 (divisão de conta
    /// de verdade) é quem vai LER estes dois campos.
    /// </remarks>
    public partial class AddTableSessionSplitPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "split_mode",
                table: "table_session",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "split_people",
                table: "table_session",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "split_mode",
                table: "table_session");

            migrationBuilder.DropColumn(
                name: "split_people",
                table: "table_session");
        }
    }
}
