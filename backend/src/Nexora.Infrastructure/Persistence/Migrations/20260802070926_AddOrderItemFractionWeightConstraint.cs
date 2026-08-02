using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// US-013 (Pizza meio a meio com frações), §8: "A soma de <c>weight</c> das frações de um item
    /// deve ser exatamente 1,0 — garantido por constraint de banco, não por validação de
    /// aplicação." Um <c>CHECK</c> simples não serve (não agrega linhas irmãs — não existe
    /// <c>CHECK</c> multilinha no Postgres), então a garantia é uma função + trigger, seguindo o
    /// estilo de migration SQL já usado no projeto (<c>migrationBuilder.Sql(...)</c>, ver
    /// <c>20260801222751_EnableRowLevelSecurity</c> e <c>20260802005842_CreateAuthLookupUserFunction</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Por que <c>CONSTRAINT TRIGGER ... DEFERRABLE INITIALLY DEFERRED</b> (não um <c>TRIGGER</c>
    /// comum): um item meio a meio é gravado como duas ou mais linhas separadas de
    /// <c>order_item_fraction</c>, cada uma em seu próprio <c>INSERT</c> (é como o EF Core grava
    /// uma coleção — um <c>INSERT</c> por entidade rastreada). Um trigger <c>AFTER EACH ROW</c>
    /// não-adiado validaria a soma logo após o PRIMEIRO <c>INSERT</c> (soma parcial, ex.: 0,5 de
    /// duas frações de 0,5 cada) e rejeitaria uma transação perfeitamente válida antes mesmo do
    /// segundo <c>INSERT</c> acontecer. <c>DEFERRABLE INITIALLY DEFERRED</c> adia a checagem para
    /// o <c>COMMIT</c> da transação — momento em que TODAS as linhas do item já foram gravadas
    /// (o <c>SaveChangesAsync</c> do EF Core, ou o <c>TransactionBehavior</c> do MediatR quando
    /// há mais de uma alteração, sempre executa dentro de uma única transação).
    /// </para>
    /// <para>
    /// A checagem só dispara quando existe ao menos uma linha para o <c>order_item_id</c> afetado
    /// (soma = 0 quando a última fração de um item é apagada é aceita — item deixou de ser meio a
    /// meio, não é mais um caso desta constraint). A função roda como <c>SECURITY INVOKER</c>
    /// (padrão) — o <c>SELECT</c> interno respeita RLS (ADR-004) com o mesmo <c>app.tenant_id</c>
    /// já definido pela sessão que disparou o <c>INSERT</c>/<c>UPDATE</c>/<c>DELETE</c>, então o
    /// <c>SUM</c> nunca soma linhas de outro tenant.
    /// </para>
    /// </remarks>
    public partial class AddOrderItemFractionWeightConstraint : Migration
    {
        private const string FunctionName = "check_order_item_fraction_weight_sum";
        private const string TriggerName = "trg_order_item_fraction_weight_sum";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                CREATE OR REPLACE FUNCTION {FunctionName}() RETURNS trigger AS $$
                DECLARE
                  affected_order_item_id uuid;
                  weight_sum numeric;
                BEGIN
                  IF TG_OP = 'DELETE' THEN
                    affected_order_item_id := OLD.order_item_id;
                  ELSE
                    affected_order_item_id := NEW.order_item_id;
                  END IF;

                  SELECT COALESCE(SUM(weight), 0) INTO weight_sum
                  FROM order_item_fraction
                  WHERE order_item_id = affected_order_item_id;

                  -- weight_sum = 0 -> nenhuma fração restante para este item (ex.: a última linha
                  -- acabou de ser apagada) -> não é mais um item meio a meio, nada a validar.
                  IF weight_sum <> 0 AND weight_sum <> 1.0 THEN
                    RAISE EXCEPTION
                      'A soma dos pesos das frações do item % deve ser exatamente 1.0 (atual: %)',
                      affected_order_item_id, weight_sum
                      USING ERRCODE = 'check_violation';
                  END IF;

                  RETURN NULL; -- resultado ignorado em trigger AFTER
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql(
                $"""
                DROP TRIGGER IF EXISTS {TriggerName} ON order_item_fraction;
                CREATE CONSTRAINT TRIGGER {TriggerName}
                AFTER INSERT OR UPDATE OR DELETE ON order_item_fraction
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION {FunctionName}();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerName} ON order_item_fraction;");
            migrationBuilder.Sql($"DROP FUNCTION IF EXISTS {FunctionName}();");
        }
    }
}
