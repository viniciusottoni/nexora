using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// US-021/US-022: adiciona <c>guest_count_confirmed</c> em <c>table_session</c> (contagem de
    /// pessoas pendente de confirmação do garçom quando a sessão é aberta por QR).
    ///
    /// [NOTA] <c>dotnet ef migrations add</c> também detectou <c>color</c>/<c>is_bottleneck</c>
    /// (station), <c>critical_minutes</c>/<c>warn_minutes</c> (product_variant),
    /// <c>auto_restore</c> (product) e os índices/constraints de
    /// <c>EnforceCatalogPriceAndStationIntegrity</c> como "pendentes" — <c>AppDbContextModelSnapshot.cs</c>
    /// estava desatualizado em relação às migrations <c>AddCatalogE01StationAndPrepTimeFields</c>,
    /// <c>PersistAvailabilityAutoRestore</c> e <c>EnforceCatalogPriceAndStationIntegrity</c>
    /// (arquivos de migration existem e já rodaram no banco; só o snapshot não tinha sido
    /// regenerado — drift pré-existente, não introduzido por esta tarefa). Essas operações foram
    /// removidas manualmente deste Up()/Down() — reaplicá-las quebraria qualquer banco que já
    /// tenha essas três migrations aplicadas ("column already exists"). O snapshot já foi
    /// corrigido por este mesmo <c>dotnet ef migrations add</c> (ver AppDbContextModelSnapshot.cs)
    /// — a correção do snapshot é mantida porque é só metadado de design-time (não é executada
    /// contra o banco), mas o Up()/Down() abaixo contém só a coluna genuinamente nova desta
    /// história.
    /// </remarks>
    public partial class AddTableSessionGuestCountConfirmed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "guest_count_confirmed",
                table: "table_session",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "guest_count_confirmed",
                table: "table_session");
        }
    }
}
