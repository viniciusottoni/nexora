using System.Data;
using System.Data.Common;

namespace Nexora.UnitTests.Persistence.Fakes;

/// <summary>
/// Duplo de teste mínimo de <see cref="DbConnection"/> usado para exercitar
/// <c>TenantConnectionInterceptor</c> sem depender de um Postgres real nem do provider Npgsql —
/// grava todo <see cref="FakeDbCommand"/> criado por <see cref="CreateDbCommand"/> em
/// <see cref="ExecutedCommands"/> para o teste inspecionar <c>CommandText</c> e call count.
/// </summary>
public sealed class FakeDbConnection : DbConnection
{
    public List<FakeDbCommand> ExecutedCommands { get; } = new();

    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "fake";
    public override string DataSource => "fake";
    public override string ServerVersion => "0";
    public override ConnectionState State { get; }

    public FakeDbConnection(ConnectionState state = ConnectionState.Open)
    {
        State = state;
    }

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close()
    {
    }

    public override void Open()
    {
    }

    protected override DbCommand CreateDbCommand()
    {
        var command = new FakeDbCommand();
        ExecutedCommands.Add(command);
        return command;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotSupportedException("Não usado por TenantConnectionInterceptor.");
}
