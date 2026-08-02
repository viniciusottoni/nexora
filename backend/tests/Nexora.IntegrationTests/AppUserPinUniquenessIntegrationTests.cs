using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-004, gap "índice de unicidade de PIN aponta pro campo errado" — <c>uq_app_user_pin</c> era
/// definido sobre <c>pin_hash</c> (Argon2id com salt aleatório: dois PINs IGUAIS produzem hashes
/// DIFERENTES, então o índice nunca detectava colisão nenhuma). A correção (ver
/// <c>AppUserConfiguration.cs</c> + migration <c>FixPinUniquenessIndex</c>) move o índice para
/// <c>pin_lookup</c> — o digest HMAC determinístico do PIN, que É igual para dois usuários com o
/// mesmo PIN. Roda contra Postgres real (Testcontainers) para provar a restrição de banco de
/// verdade, não uma simulação em memória (RLS/constraints não podem ser simulados — CLAUDE.md).
/// </summary>
[Collection("Postgres")]
public sealed class AppUserPinUniquenessIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AppUserPinUniquenessIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Dois_Usuarios_Ativos_Com_O_Mesmo_Pin_No_Mesmo_Tenant_Sao_Rejeitados_Pelo_Banco()
    {
        var tenantId = await SeedTenantAsync();
        const string sharedPinLookup = "digest-hmac-determinístico-do-mesmo-pin";

        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            db.Users.Add(AppUser.Create(
                tenantId, "Operador 1", email: null, passwordHash: null,
                pinHash: "hash-argon2-salt-a-do-operador-1", pinLookup: sharedPinLookup));
            await db.SaveChangesAsync();
        }

        await using var conflictingDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        conflictingDb.Users.Add(AppUser.Create(
            // PinHash é DIFERENTE do primeiro usuário (Argon2id com salt aleatório — mesmo PIN em
            // texto claro, hash diferente por construção); é exatamente por isso que o índice
            // antigo (sobre pin_hash) nunca detectava esta colisão. PinLookup é o MESMO — o digest
            // HMAC determinístico do mesmo PIN — e é sobre ele que o índice corrigido atua.
            tenantId, "Operador 2", email: null, passwordHash: null,
            pinHash: "hash-argon2-salt-b-do-operador-2", pinLookup: sharedPinLookup));

        var act = async () => await conflictingDb.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "uq_app_user_pin (agora sobre pin_lookup) deve rejeitar o segundo usuário ativo com o mesmo PIN no mesmo tenant");
    }

    [Fact]
    public async Task Usuarios_Com_Pin_Diferente_No_Mesmo_Tenant_Sao_Aceitos()
    {
        var tenantId = await SeedTenantAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        db.Users.Add(AppUser.Create(tenantId, "Operador 1", email: null, passwordHash: null, pinHash: "hash-1", pinLookup: "lookup-1"));
        db.Users.Add(AppUser.Create(tenantId, "Operador 2", email: null, passwordHash: null, pinHash: "hash-2", pinLookup: "lookup-2"));

        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task O_Mesmo_Pin_Em_Tenants_Diferentes_E_Permitido()
    {
        var tenantA = await SeedTenantAsync();
        var tenantB = await SeedTenantAsync();
        const string sharedPinLookup = "digest-repetido-entre-tenants";

        await using (var dbA = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA)))
        {
            dbA.Users.Add(AppUser.Create(tenantA, "Operador A", email: null, passwordHash: null, pinHash: "hash-a", pinLookup: sharedPinLookup));
            await dbA.SaveChangesAsync();
        }

        await using var dbB = _fixture.CreateAppDbContext(new StaticTenantContext(tenantB));
        dbB.Users.Add(AppUser.Create(tenantB, "Operador B", email: null, passwordHash: null, pinHash: "hash-b", pinLookup: sharedPinLookup));

        var act = async () => await dbB.SaveChangesAsync();

        await act.Should().NotThrowAsync("o índice é composto por (tenant_id, pin_lookup) — o mesmo PIN em OUTRO tenant não colide");
    }

    private async Task<Guid> SeedTenantAsync()
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        return tenantId;
    }
}
