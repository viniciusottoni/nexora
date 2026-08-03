using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// E-09/US-090, cenário Gherkin "Imutabilidade" — prova contra um PostgreSQL real que
/// <c>UPDATE</c>/<c>DELETE</c> em <c>audit_log</c> são recusados pelo BANCO (migration
/// <c>PartitionAuditLogAndRestrictMutation</c>), não apenas evitados por convenção de código. Uma
/// tabela em que a aplicação consegue dar <c>UPDATE</c> é só um log, não uma trilha de auditoria
/// (US-090 §2).
/// </summary>
[Collection("Postgres")]
public sealed class AuditLogImmutabilityTests
{
    private readonly PostgresFixture _fixture;

    public AuditLogImmutabilityTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Update_Em_Audit_Log_E_Recusado_Pelo_Banco()
    {
        var tenantId = await SeedTenantWithOneAuditEntryAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var act = async () => await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE audit_log SET reason = 'alterado' WHERE tenant_id = {tenantId}");

        var exception = await act.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task Delete_Em_Audit_Log_E_Recusado_Pelo_Banco()
    {
        var tenantId = await SeedTenantWithOneAuditEntryAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var act = async () => await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM audit_log WHERE tenant_id = {tenantId}");

        var exception = await act.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    private async Task<Guid> SeedTenantWithOneAuditEntryAsync()
    {
        var tenantId = Guid.NewGuid();

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant"));
            await seedDb.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            db.AuditLogs.Add(AuditLog.Create(
                tenantId, action: "TEST_ACTION", entity: "test", occurredAt: DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        return tenantId;
    }
}
