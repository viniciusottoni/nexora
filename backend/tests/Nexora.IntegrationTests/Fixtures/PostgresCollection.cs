using Xunit;

namespace Nexora.IntegrationTests.Fixtures;

/// <summary>
/// Um único container/migration para toda a suíte de isolamento (US-001) — subir Postgres e
/// aplicar 65 tabelas com RLS por classe de teste seria lento sem necessidade; cada teste isola
/// seus próprios dados com um <see cref="Guid.NewGuid"/> por tenant, então compartilhar o
/// container entre classes é seguro.
/// </summary>
[CollectionDefinition("Postgres")]
public sealed class PostgresFixtures : ICollectionFixture<PostgresFixture>
{
}
