using System.Data;
using System.Data.Common;

namespace Nexora.UnitTests.Persistence.Fakes;

/// <summary>
/// Duplo de teste mínimo de <see cref="DbCommand"/> — <see cref="TenantConnectionInterceptor"/>
/// só chama <see cref="ExecuteNonQuery"/>/<see cref="DbCommand.ExecuteNonQueryAsync(CancellationToken)"/>
/// depois de definir <see cref="CommandText"/>, então o resto dos membros abstratos existe só
/// para compilar. Evita depender de NSubstitute para <see cref="DbConnection"/>/<see cref="DbCommand"/>
/// (proxy de classe abstrata com <c>CreateDbCommand</c>/<c>ExecuteNonQuery</c> protegidos é frágil
/// nas ferramentas de mock disponíveis no projeto).
/// </summary>
public sealed class FakeDbCommand : DbCommand
{
    public int ExecuteNonQueryCallCount { get; private set; }

    public override string? CommandText { get; set; }
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();
    protected override DbTransaction? DbTransaction { get; set; }
    public override bool DesignTimeVisible { get; set; }

    public override void Cancel()
    {
    }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        throw new NotSupportedException("Não usado por TenantConnectionInterceptor.");

    public override int ExecuteNonQuery()
    {
        ExecuteNonQueryCallCount++;
        return 0;
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        ExecuteNonQueryCallCount++;
        return Task.FromResult(0);
    }

    public override object? ExecuteScalar() => null;

    public override void Prepare()
    {
    }

    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<object> _items = new();

        public override int Count => _items.Count;
        public override object SyncRoot => this;

        public override int Add(object value)
        {
            _items.Add(value);
            return _items.Count - 1;
        }

        public override void AddRange(Array values) => _items.AddRange(values.Cast<object>());
        public override void Clear() => _items.Clear();
        public override bool Contains(string value) => false;
        public override bool Contains(object value) => _items.Contains(value);
        public override void CopyTo(Array array, int index) => throw new NotSupportedException();
        public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();
        protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];
        protected override DbParameter GetParameter(string parameterName) =>
            throw new NotSupportedException();
        public override int IndexOf(string parameterName) => -1;
        public override int IndexOf(object value) => _items.IndexOf(value);
        public override void Insert(int index, object value) => _items.Insert(index, value);
        public override void Remove(object value) => _items.Remove(value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => throw new NotSupportedException();
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) =>
            throw new NotSupportedException();
    }

    private sealed class FakeDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string? ParameterName { get; set; }
        public override int Size { get; set; }
        public override string? SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }

        public override void ResetDbType()
        {
        }
    }
}
