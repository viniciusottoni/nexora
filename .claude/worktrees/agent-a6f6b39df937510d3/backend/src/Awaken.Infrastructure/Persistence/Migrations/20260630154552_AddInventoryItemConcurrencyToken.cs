using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Awaken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// US-227 / RN-006: esta migration NÃO altera o schema real. "xmin" é uma
    /// coluna de sistema já existente em toda tabela PostgreSQL (MVCC) — o
    /// scaffold padrão do EF Core gerou um AddColumn/DropColumn para ela por
    /// desconhecer esse mapeamento especial em uma tabela pré-existente (o
    /// mesmo truque em GoldWalletConfiguration não precisou de migration
    /// porque a tabela era nova naquele momento). Os corpos de Up/Down foram
    /// esvaziados propositalmente — o único efeito desta migration é
    /// atualizar o model snapshot do EF Core para refletir o mapeamento de
    /// InventoryItem.Version → xmin, eliminando o PendingModelChangesWarning.
    /// </remarks>
    public partial class AddInventoryItemConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intencionalmente vazio — "xmin" já existe como coluna de sistema
            // do PostgreSQL; nenhuma alteração real de schema é necessária.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intencionalmente vazio — ver comentário em Up().
        }
    }
}
