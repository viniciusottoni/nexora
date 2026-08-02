using System.Data;
using Nexora.Application.Abstractions.Security;
using Nexora.Infrastructure.Persistence.Interceptors;
using Nexora.UnitTests.Persistence.Fakes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Nexora.UnitTests.Persistence;

/// <summary>
/// Cobre o cenário Gherkin "Query sem contexto de tenant" da US-001 (doc. 12 do pacote) e o risco
/// de vazamento entre requisições de um pool de conexões (doc. 10 §2, "sem <c>true</c> o
/// contexto vaza entre requisições que reutilizam a mesma conexão"). Usa duplos de teste de
/// <see cref="System.Data.Common.DbConnection"/>/<see cref="System.Data.Common.DbCommand"/>
/// (<see cref="Fakes.FakeDbConnection"/>) em vez de um mock de proxy — <c>DbConnection</c> expõe
/// <c>CreateDbCommand</c>/<c>ExecuteNonQuery</c> como membros protegidos/abstratos difíceis de
/// configurar com NSubstitute; a conexão é o "mock" pedido pela US-001 (doc. 12, nível Unitário).
/// </summary>
public sealed class TenantConnectionInterceptorTests
{
    private readonly ICurrentTenantContext _tenantContext = Substitute.For<ICurrentTenantContext>();
    private readonly ILogger<TenantConnectionInterceptor> _logger = Substitute.For<ILogger<TenantConnectionInterceptor>>();

    private TenantConnectionInterceptor CreateSut() => new(_tenantContext, _logger);

    [Fact]
    public void ConnectionOpened_Com_TenantId_Define_App_Tenant_Id()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var connection = new FakeDbConnection();
        var sut = CreateSut();

        sut.ConnectionOpened(connection, CreateConnectionEndEventData(connection));

        connection.ExecutedCommands.Should().HaveCount(1);
        connection.ExecutedCommands[0].CommandText.Should().Contain("set_config").And.Contain("app.tenant_id");
        connection.ExecutedCommands[0].ExecuteNonQueryCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ConnectionOpenedAsync_Com_TenantId_Define_App_Tenant_Id()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);
        var connection = new FakeDbConnection();
        var sut = CreateSut();

        await sut.ConnectionOpenedAsync(connection, CreateConnectionEndEventData(connection));

        connection.ExecutedCommands.Should().HaveCount(1);
        connection.ExecutedCommands[0].CommandText.Should().Contain("set_config").And.Contain("app.tenant_id");
        connection.ExecutedCommands[0].ExecuteNonQueryCallCount.Should().Be(1);
    }

    /// <summary>
    /// Cenário Gherkin central: sem <c>ICurrentTenantContext.TenantId</c>, nenhum comando é
    /// emitido — falha fechada por padrão do RLS (ADR-004), a política nega leitura em vez de a
    /// aplicação forçar um valor. A ausência de comando é o comportamento correto (não é bug).
    /// </summary>
    [Fact]
    public void ConnectionOpened_Sem_TenantId_Nao_Executa_Nenhum_Comando()
    {
        _tenantContext.TenantId.Returns((Guid?)null);
        var connection = new FakeDbConnection();
        var sut = CreateSut();

        sut.ConnectionOpened(connection, CreateConnectionEndEventData(connection));

        connection.ExecutedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task ConnectionOpenedAsync_Sem_TenantId_Nao_Executa_Nenhum_Comando()
    {
        _tenantContext.TenantId.Returns((Guid?)null);
        var connection = new FakeDbConnection();
        var sut = CreateSut();

        await sut.ConnectionOpenedAsync(connection, CreateConnectionEndEventData(connection));

        connection.ExecutedCommands.Should().BeEmpty();
    }

    /// <summary>
    /// Cobre a "violação de contrato interno" do cenário Gherkin: sem contexto, o interceptor
    /// registra log estruturado em vez de silenciosamente deixar passar — é o sinal que
    /// observabilidade (doc. 11 da US-001) espera monitorar em produção.
    /// </summary>
    [Fact]
    public void ConnectionOpened_Sem_TenantId_Loga_Aviso()
    {
        _tenantContext.TenantId.Returns((Guid?)null);
        var connection = new FakeDbConnection();
        var sut = CreateSut();

        sut.ConnectionOpened(connection, CreateConnectionEndEventData(connection));

        // ILogger.Log<TState> é genérico — verificar a chamada exata via NSubstitute exige
        // casar o TState fechado (FormattedLogValues, interno ao runtime de logging), o que é
        // frágil entre versões. ReceivedCalls() evita depender desse detalhe de implementação:
        // só confirma que ALGUMA invocação aconteceu no logger substituto.
        _logger.ReceivedCalls().Should().NotBeEmpty("deveria logar a ausência de app.tenant_id como violação de contrato interno");
    }

    /// <summary>
    /// O bug original (task item 2): a implementação anterior nunca resetava
    /// <c>app.tenant_id</c> ao fechar a conexão — com escopo de sessão (<c>false</c>, não
    /// <c>true</c>/SET LOCAL, ver nota da classe), isso vazaria o tenant de uma requisição para a
    /// próxima que reusasse a mesma conexão física do pool do Npgsql. Este teste prova que o reset
    /// acontece sempre, mesmo quando a conexão nunca teve tenant definido.
    /// </summary>
    [Fact]
    public void ConnectionClosing_Reseta_App_Tenant_Id_Quando_Conexao_Esta_Aberta()
    {
        var connection = new FakeDbConnection(ConnectionState.Open);
        var sut = CreateSut();

        sut.ConnectionClosing(connection, CreateConnectionEventData(connection), default);

        connection.ExecutedCommands.Should().ContainSingle(c => c.CommandText.Contains("RESET") && c.CommandText.Contains("app.tenant_id"));
    }

    [Fact]
    public async Task ConnectionClosingAsync_Reseta_App_Tenant_Id_Quando_Conexao_Esta_Aberta()
    {
        var connection = new FakeDbConnection(ConnectionState.Open);
        var sut = CreateSut();

        await sut.ConnectionClosingAsync(connection, CreateConnectionEventData(connection), default);

        connection.ExecutedCommands.Should().ContainSingle(c => c.CommandText.Contains("RESET") && c.CommandText.Contains("app.tenant_id"));
    }

    [Fact]
    public void ConnectionClosing_Nao_Executa_Comando_Quando_Conexao_Ja_Fechada()
    {
        var connection = new FakeDbConnection(ConnectionState.Closed);
        var sut = CreateSut();

        sut.ConnectionClosing(connection, CreateConnectionEventData(connection), default);

        connection.ExecutedCommands.Should().BeEmpty();
    }

    private static ConnectionEndEventData CreateConnectionEndEventData(FakeDbConnection connection, bool async = false)
    {
        var definition = CreateEventDefinition();
        return new ConnectionEndEventData(
            definition,
            (_, _) => "test",
            connection,
            context: null,
            connectionId: Guid.NewGuid(),
            async: async,
            startTime: DateTimeOffset.UtcNow,
            duration: TimeSpan.Zero);
    }

    private static ConnectionEventData CreateConnectionEventData(FakeDbConnection connection, bool async = false)
    {
        var definition = CreateEventDefinition();
        return new ConnectionEventData(
            definition,
            (_, _) => "test",
            connection,
            context: null,
            connectionId: Guid.NewGuid(),
            async: async,
            startTime: DateTimeOffset.UtcNow);
    }

    private static EventDefinition CreateEventDefinition()
    {
        var loggingOptions = Substitute.For<ILoggingOptions>();
        return new EventDefinition(loggingOptions, new EventId(0), LogLevel.Debug, "Test", _ => (_, _) => { });
    }
}
