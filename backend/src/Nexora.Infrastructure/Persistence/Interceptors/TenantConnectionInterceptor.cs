using System.Data;
using System.Data.Common;
using Nexora.Application.Abstractions.Security;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Nexora.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Aplica <c>app.tenant_id</c> a cada conexão aberta pelo EF Core, para que o RLS do PostgreSQL
/// (ADR-004) isole os dados por tenant — mecanismo obrigatório e único ponto de entrada de
/// contexto de tenant no banco (ver ADR-038). Sem <c>ICurrentTenantContext.TenantId</c> definido
/// (ex.: endpoints públicos de login), nenhum valor é fixado e o banco nega leitura de qualquer
/// tabela com RLS — falha fechada por padrão.
/// </summary>
/// <remarks>
/// <para>
/// <b>Desvio deliberado do SQL literal do ADR-004/Docs/Domain/10</b> (<c>set_config(...,
/// true)</c>, equivalente a <c>SET LOCAL</c>): esse parâmetro só é válido dentro de uma
/// transação explícita (<c>BEGIN</c>) — fora dela, o Postgres trata cada comando isolado como sua
/// própria transação implícita, e o valor "local" desaparece assim que o comando de
/// <c>set_config</c> termina, ANTES da query real (que chega como um roundtrip separado) ser
/// executada. Como o EF Core não envolve toda leitura em uma transação explícita — só
/// <see cref="Nexora.Application.Abstractions.Behaviors.TransactionBehavior{TRequest,TResponse}"/>
/// abre uma, e só para comandos — usar o parâmetro <c>true</c> aqui faria o RLS negar
/// silenciosamente toda consulta autenticada, não só as sem contexto.
/// </para>
/// <para>
/// Em vez disso, o valor é fixado com escopo de <b>sessão</b> (<c>set_config(..., false)</c>,
/// terceiro parâmetro <c>false</c>) em <see cref="ConnectionOpened"/>/<see cref="ConnectionOpenedAsync"/>
/// e explicitamente resetado (<c>RESET app.tenant_id</c>) em
/// <see cref="ConnectionClosing"/>/<see cref="ConnectionClosingAsync"/>, ANTES de a conexão física
/// voltar ao pool do Npgsql. Isso preserva exatamente a garantia que o ADR-004 buscava com
/// <c>true</c> — nenhum valor de uma requisição sobrevive para a próxima que reusar a mesma
/// conexão física — sem depender de uma transação ambiente que este código não controla.
/// Ver nota equivalente em <see cref="Persistence.AppDbContext.SetTenantContextAsync"/>
/// (<c>AppDbContext.Auth.cs</c>), que tinha o mesmo problema.
/// </para>
/// </remarks>
public sealed class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ILogger<TenantConnectionInterceptor> _logger;

    public TenantConnectionInterceptor(ICurrentTenantContext tenantContext, ILogger<TenantConnectionInterceptor> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        base.ConnectionOpened(connection, eventData);
        ApplyTenantContext(connection);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        await ApplyTenantContextAsync(connection, cancellationToken);
    }

    public override InterceptionResult ConnectionClosing(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        ResetTenantContext(connection);
        return base.ConnectionClosing(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        await ResetTenantContextAsync(connection);
        return await base.ConnectionClosingAsync(connection, eventData, result);
    }

    private void ApplyTenantContext(DbConnection connection)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            LogMissingTenantContext();
            return;
        }

        using var cmd = CreateSetTenantCommand(connection, tenantId.Value);
        cmd.ExecuteNonQuery();
    }

    private async Task ApplyTenantContextAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            LogMissingTenantContext();
            return;
        }

        await using var cmd = CreateSetTenantCommand(connection, tenantId.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Sem <c>ICurrentTenantContext.TenantId</c>, nenhum <c>SET</c> é emitido — a política RLS
    /// nega qualquer leitura/escrita em tabela de negócio por padrão (ADR-004, falha fechada).
    /// Isso é esperado nos poucos endpoints públicos documentados (login por senha/PIN, refresh) —
    /// mas também é exatamente o sintoma de um bug em qualquer outra rota, e por isso vira log
    /// estruturado (cenário "query sem contexto de tenant" da US-001, doc. 12 do pacote):
    /// verificação de contrato interno de que TODA rota de negócio autenticada resolveu um
    /// tenant antes de tocar o banco.
    /// </summary>
    private void LogMissingTenantContext() =>
        _logger.LogWarning(
            "Conexão de banco aberta sem app.tenant_id (ICurrentTenantContext.TenantId nulo). " +
            "RLS negará leitura/escrita em qualquer tabela de negócio (ADR-004, falha fechada). " +
            "Esperado apenas em endpoints públicos de autenticação — investigar se ocorreu fora deles.");

    private static DbCommand CreateSetTenantCommand(DbConnection connection, Guid tenantId)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.tenant_id', @tenantId, false)";
        if (cmd is NpgsqlCommand npgsqlCommand)
        {
            npgsqlCommand.Parameters.AddWithValue("tenantId", tenantId.ToString());
        }

        return cmd;
    }

    private static void ResetTenantContext(DbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
        {
            return;
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "RESET app.tenant_id";
            cmd.ExecuteNonQuery();
        }
        catch (DbException)
        {
            // Conexão pode já estar em estado inválido/quebrada (ex.: falha de rede) — não
            // impedir o fechamento por isso. O pool do Npgsql descarta conexões quebradas em vez
            // de reciclá-las, então não há risco de vazamento de app.tenant_id nesse caminho.
        }
    }

    private static async Task ResetTenantContextAsync(DbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
        {
            return;
        }

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "RESET app.tenant_id";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (DbException)
        {
            // Ver nota em ResetTenantContext.
        }
    }
}
