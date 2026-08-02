using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Cria <c>auth_lookup_user(citext)</c> — pré-requisito para o login por senha
    /// (<c>LoginWithPasswordCommandHandler</c>/<c>AppDbContext.Auth.FindLoginCredentialByEmailAsync</c>),
    /// que precisa achar a que tenant um e-mail pertence ANTES de <c>app.tenant_id</c> estar
    /// definido — sem isto, RLS (ADR-004, migration <c>EnableRowLevelSecurity</c>) nega qualquer
    /// leitura de <c>app_user</c> e o login por senha falha em runtime (gap encontrado pela
    /// auditoria de US-001, nunca corrigido pelas correções de US-004/plumbing).
    /// </summary>
    /// <remarks>
    /// <c>SECURITY DEFINER</c> roda com o privilégio de quem CRIOU a função (o role de migração,
    /// que é dono das tabelas) — donos de tabela ignoram RLS por padrão no Postgres, então a
    /// função vê todos os tenants independente de quem a chama. Só devolve o mínimo necessário
    /// (id/tenant/hash) para o handler decidir tenant e verificar senha; nunca expõe a tabela
    /// inteira. <c>REVOKE ... FROM PUBLIC</c> + <c>GRANT EXECUTE</c> só para <c>app_user_role</c>
    /// evita que qualquer role consiga chamá-la.
    /// </remarks>
    public partial class CreateAuthLookupUserFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION auth_lookup_user(p_email citext)
                RETURNS TABLE(user_id uuid, tenant_id uuid, password_hash text)
                LANGUAGE sql
                SECURITY DEFINER
                SET search_path = public
                AS $$
                    SELECT id, tenant_id, password_hash
                    FROM app_user
                    WHERE email = p_email AND deleted_at IS NULL
                    LIMIT 1;
                $$;
                """);

            migrationBuilder.Sql("REVOKE ALL ON FUNCTION auth_lookup_user(citext) FROM PUBLIC;");
            migrationBuilder.Sql("GRANT EXECUTE ON FUNCTION auth_lookup_user(citext) TO app_user_role, platform_admin;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS auth_lookup_user(citext);");
        }
    }
}
