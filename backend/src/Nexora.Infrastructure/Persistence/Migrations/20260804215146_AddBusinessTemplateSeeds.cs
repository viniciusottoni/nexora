using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.Application.Provisioning;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-142 "Modelos por tipo de negócio" — semeia os 4 modelos de negócio
    /// (pizzaria/hamburgueria/restaurante/lanchonete) em <c>business_template</c>, substituindo o
    /// catálogo estático de código (<c>Nexora.Domain.Provisioning.ProvisioningTemplates</c>, um
    /// <c>switch</c> que só sabia "PIZZERIA") por dado editável pela Replay sem deploy — a
    /// aplicação direta de ADR-013 que esta US existe para fazer. O conteúdo de cada modelo (config
    /// + seeds) vem de <see cref="BusinessTemplateSeedCatalog"/>/<see cref="BusinessTemplateDataMapper"/>
    /// (a MESMA dupla que <c>ProvisionTenantCommandHandler</c> usa para ler de volta em runtime) —
    /// gerado aqui, não copiado à mão, para garantir que o JSON persistido bate byte a byte com o
    /// que o mapeador espera deserializar (round-trip provado por
    /// <c>Nexora.UnitTests.Provisioning.BusinessTemplateDataMapperTests</c>).
    /// </summary>
    /// <inheritdoc />
    public partial class AddBusinessTemplateSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var seedAt = new DateTimeOffset(2026, 8, 4, 21, 51, 46, TimeSpan.Zero);

            foreach (var (id, code, name, template) in RowsToSeed())
            {
                var (configJson, seedsJson) = BusinessTemplateDataMapper.Serialize(template);

                migrationBuilder.InsertData(
                    table: "business_template",
                    columns: new[] { "id", "code", "name", "version", "config", "seeds", "is_active", "created_at", "updated_at" },
                    values: new object[] { id, code, name, 1, configJson, seedsJson, true, seedAt, seedAt });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, _, _, _) in RowsToSeed())
            {
                migrationBuilder.DeleteData(table: "business_template", keyColumn: "id", keyValue: id);
            }
        }

        private static IEnumerable<(Guid Id, string Code, string Name, Domain.Provisioning.ProvisioningTemplate Template)> RowsToSeed()
        {
            // GUIDs fixos (não Guid.CreateVersion7 em tempo de deploy) para que a migration seja
            // determinística/idempotente entre ambientes — mesma linha sempre tem o mesmo id.
            yield return (
                new Guid("85707c89-3e0a-48c8-99ef-b07d275c236b"),
                BusinessTemplateSeedCatalog.PizzeriaCode, "Pizzaria", BusinessTemplateSeedCatalog.Pizzeria());
            yield return (
                new Guid("ae257900-8450-4d23-9a15-1be18ee1d945"),
                BusinessTemplateSeedCatalog.HamburgueriaCode, "Hamburgueria", BusinessTemplateSeedCatalog.Hamburgueria());
            yield return (
                new Guid("ab7493a2-d773-42c0-a4b7-ff9be3a215e5"),
                BusinessTemplateSeedCatalog.RestauranteCode, "Restaurante", BusinessTemplateSeedCatalog.Restaurante());
            yield return (
                new Guid("04b70027-dfb2-4089-abaa-3ea88660c723"),
                BusinessTemplateSeedCatalog.LanchoneteCode, "Lanchonete", BusinessTemplateSeedCatalog.Lanchonete());
        }
    }
}
