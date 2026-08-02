using Nexora.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// Implementação dos dois métodos de bootstrap de autenticação de <see cref="IApplicationDbContext"/>
/// — porta de <c>auth_lookup_user()</c> e do helper <c>setTenant(tx, tenantId)</c>
/// (apps/api-cloud/src/modules/auth/prisma-password-auth.repository.ts), usados só pelo módulo
/// Auth do cloud nos dois pontos em que uma consulta precisa acontecer antes do RLS reconhecer o
/// tenant (ADR-004: sem <c>app.tenant_id</c> definido, o banco não retorna nada).
/// </summary>
public partial class AppDbContext
{
    public async Task<LoginCredentialLookup?> FindLoginCredentialByEmailAsync(string email, CancellationToken cancellationToken)
    {
        // auth_lookup_user(citext) é SECURITY DEFINER (migration CreateAuthLookupUserFunction) —
        // roda com o privilégio de quem a criou (dono das tabelas, isento de RLS por padrão), o
        // que permite achar o tenant do e-mail ANTES de app.tenant_id estar definido (ADR-004:
        // sem contexto, RLS nega leitura de app_user).
        // Sem alias: EFCore.NamingConventions aplica snake_case a QUALQUER SqlQueryRaw<T> (não só a
        // entidades mapeadas), então a correspondência de coluna espera user_id/tenant_id/password_hash
        // — exatamente os nomes que a função já devolve. Um alias "UserId" (PascalCase entre aspas)
        // preserva o case literal e quebra esse casamento (achado ao rodar este método pela
        // primeira vez contra Postgres real, em LoginWithPasswordIntegrationTests).
        var results = await Database
            .SqlQueryRaw<LoginCredentialLookup>(
                """
                SELECT user_id, tenant_id, password_hash
                FROM auth_lookup_user({0}::citext)
                """,
                email)
            .ToListAsync(cancellationToken);

        return results.FirstOrDefault();
    }

    // NOTA: terceiro parâmetro `false` (escopo de sessão), não `true` (SET LOCAL/escopo de
    // transação) como no SQL literal do ADR-004 — mesmo motivo documentado em
    // Persistence/Interceptors/TenantConnectionInterceptor.cs: `true` só sobrevive dentro de uma
    // transação explícita, e os chamadores deste método (LoginWithPassword, RefreshToken,
    // ProvisionTenant, EmailOutboxDeliveryWorker) fazem operações seguintes fora de uma
    // transação aberta por eles — o valor "local" desapareceria antes de ser lido.
    //
    // CORREÇÃO (US-002, encontrada ao escrever o primeiro teste de integração de
    // ProvisionTenantCommandHandler contra Postgres real): `ExecuteSqlInterpolatedAsync` sozinho
    // abre e FECHA a conexão ao final da própria chamada — e `TenantConnectionInterceptor` reseta
    // `app.tenant_id` em toda `ConnectionClosing` (para não vazar entre requisições que reusam a
    // mesma conexão física do pool do Npgsql). Resultado: o valor fixado aqui desaparecia antes
    // da operação seguinte no MESMO handler (a próxima query, ou o `SaveChangesAsync` do
    // `TransactionBehavior`) rodar — RLS negava tudo, silenciosamente até o primeiro teste
    // contra banco real (nenhum destes fluxos era coberto por Testcontainers antes desta tarefa).
    // `Database.OpenConnectionAsync` abre a conexão com contagem de referência do próprio EF
    // Core: chamadas subsequentes (a query seguinte, o `SaveChangesAsync`) reusam a MESMA conexão
    // já aberta em vez de abrir/fechar a sua própria, então `ConnectionClosing` só dispara de
    // verdade quando o `AppDbContext` inteiro é descartado ao fim do escopo/requisição — ponto em
    // que o RESET já não importa, porque as operações deste handler já terminaram.
    public async Task SetTenantContextAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await Database.OpenConnectionAsync(cancellationToken);
        await Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.tenant_id', {tenantId.ToString()}, false)",
            cancellationToken);
    }
}
