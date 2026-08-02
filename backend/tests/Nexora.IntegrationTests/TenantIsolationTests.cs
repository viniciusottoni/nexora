using Nexora.Domain.Finance;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Prova os cinco cenários Gherkin da US-001 contra um PostgreSQL real (Testcontainers), com as
/// migrations reais aplicadas (<c>InitialCreate</c> + <c>EnableRowLevelSecurity</c>) e conectado
/// pelo papel <c>app_user_role</c> — nunca pelo superusuário do container, que ignora RLS
/// (ver <see cref="Fixtures.PostgresFixture"/>). Usa <c>expense_category</c> como tabela de
/// negócio representativa (schema simples, <c>tenant_id NOT NULL</c>, sem dependências de FK
/// extras) — a política <c>tenant_isolation</c> é idêntica nas 65 tabelas cobertas pela migration
/// (mesma <c>DO $$ FOREACH $$</c>), então provar em uma prova o desenho, não o dado.
/// </summary>
[Collection("Postgres")]
public sealed class TenantIsolationTests
{
    private readonly PostgresFixture _fixture;

    public TenantIsolationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário: "Isolamento imposto pelo banco".</summary>
    [Fact]
    public async Task Tenant_So_Ve_Proprios_Registros_Sem_Where_De_Aplicacao()
    {
        var (tenantA, tenantB) = await SeedTwoTenantsWithOneExpenseCategoryEachAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA));

        // Nenhum .Where(x => x.TenantId == tenantA) aqui de propósito — é exatamente isso que a
        // US-001 promete não ser mais necessário.
        var rows = await db.ExpenseCategories.ToListAsync();

        rows.Should().ContainSingle();
        rows[0].TenantId.Should().Be(tenantA);
    }

    /// <summary>Cenário: "Query sem contexto de tenant" — nenhuma linha retorna, falha fechada.</summary>
    [Fact]
    public async Task Query_Sem_Contexto_De_Tenant_Nao_Retorna_Nenhuma_Linha()
    {
        await SeedTwoTenantsWithOneExpenseCategoryEachAsync();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);

        var rows = await db.ExpenseCategories.ToListAsync();

        rows.Should().BeEmpty();
    }

    /// <summary>
    /// Cenário: "Escrita com tenant divergente do token" — a política <c>WITH CHECK</c> recusa o
    /// INSERT antes de ele chegar ao disco, mesmo a aplicação tentando gravar.
    /// </summary>
    [Fact]
    public async Task Insert_Com_TenantId_Divergente_E_Recusado_Pelo_Banco()
    {
        var tenantA = await SeedSingleTenantAsync();
        var tenantB = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA));
        db.ExpenseCategories.Add(ExpenseCategory.Create(tenantB, "Categoria de outro tenant", ExpenseGroup.Fixed));

        var act = async () => await db.SaveChangesAsync();

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        var message = exception.Which.InnerException is { } inner ? inner.Message : exception.Which.Message;
        message.Should().Contain("row-level security", "a política WITH CHECK deve recusar o INSERT com tenant_id divergente");
    }

    /// <summary>
    /// Cenário: "Isolamento no servidor local" — o edge é single-tenant por instalação
    /// (<c>ICurrentTenantContext.TenantId</c> nunca muda em tempo de execução, ADR-004), mas o
    /// código e o RLS são idênticos aos da nuvem. Usar o MESMO <see cref="StaticTenantContext"/>
    /// (troca só o dado, não o comportamento) prova que nenhuma ramificação de código diferencia
    /// edge de cloud — exatamente o que a US exige ("comportamento idêntico ao da nuvem").
    /// </summary>
    [Fact]
    public async Task Edge_Single_Tenant_Se_Comporta_Identico_A_Nuvem()
    {
        var (tenantA, _) = await SeedTwoTenantsWithOneExpenseCategoryEachAsync();
        var fixedEdgeTenant = new StaticTenantContext(tenantA); // instalação fixa — nunca muda

        await using var readDb = _fixture.CreateAppDbContext(fixedEdgeTenant);
        var rows = await readDb.ExpenseCategories.ToListAsync();
        rows.Should().ContainSingle().Which.TenantId.Should().Be(tenantA);

        await using var writeDb = _fixture.CreateAppDbContext(fixedEdgeTenant);
        writeDb.ExpenseCategories.Add(ExpenseCategory.Create(Guid.NewGuid(), "Outro tenant no edge", ExpenseGroup.Variable));

        var act = async () => await writeDb.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private async Task<Guid> SeedSingleTenantAsync()
    {
        var tenantId = Guid.NewGuid();
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant"));
        await db.SaveChangesAsync();
        return tenantId;
    }

    private async Task<(Guid TenantA, Guid TenantB)> SeedTwoTenantsWithOneExpenseCategoryEachAsync()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // tenant é raiz global sem RLS (ADR-004) — funciona sem contexto de tenant definido.
        await using (var db = _fixture.CreateAppDbContext(tenantContext: null))
        {
            db.Tenants.Add(Tenant.Create(tenantA, $"tenant-a-{tenantA:N}", "Tenant A"));
            db.Tenants.Add(Tenant.Create(tenantB, $"tenant-b-{tenantB:N}", "Tenant B"));
            await db.SaveChangesAsync();
        }

        // expense_category TEM RLS — cada INSERT precisa do interceptor real com o tenant certo
        // no contexto (uma instância de contexto por tenant, exatamente como duas requisições
        // HTTP diferentes teriam duas instâncias de ICurrentTenantContext diferentes).
        await using (var dbA = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA)))
        {
            dbA.ExpenseCategories.Add(ExpenseCategory.Create(tenantA, "Categoria A", ExpenseGroup.Fixed));
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = _fixture.CreateAppDbContext(new StaticTenantContext(tenantB)))
        {
            dbB.ExpenseCategories.Add(ExpenseCategory.Create(tenantB, "Categoria B", ExpenseGroup.Fixed));
            await dbB.SaveChangesAsync();
        }

        return (tenantA, tenantB);
    }
}
