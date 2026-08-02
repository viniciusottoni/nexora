using System.Text.Json;
using Nexora.Application.Auth.Shared;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-004, gap "encerramento de sessão inativa configurável — 100% não implementado". Cobre
/// <see cref="AuthSessionActivityGuard"/> — chamado por <c>SessionActivityMiddleware</c>
/// (<c>Api.Edge</c>/<c>Api.Cloud</c>) a cada requisição autenticada — contra Postgres real
/// (Testcontainers), a mesma <see cref="PostgresFixture"/> da US-001/US-005.
/// </summary>
[Collection("Postgres")]
public sealed class AuthSessionActivityGuardIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AuthSessionActivityGuardIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Cenário Gherkin da correção: "sessão sem atividade há mais tempo que o timeout configurado é
    /// rejeitada na próxima requisição". <c>AuthSession</c> não expõe API de domínio para "voltar no
    /// tempo" de propósito (<c>RecordActivity()</c> sempre grava <c>UtcNow</c> — nenhum comando de
    /// negócio deveria conseguir mentir sobre a última atividade real); backdatar via SQL bruto é a
    /// única forma de simular passagem de tempo aqui sem um <c>Task.Delay</c> de minutos no teste.
    /// </summary>
    [Fact]
    public async Task Sessao_Inativa_Alem_Do_Timeout_Configurado_E_Rejeitada_Na_Proxima_Requisicao()
    {
        var tenantId = Guid.NewGuid();
        Guid sessionId;

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await seedDb.SaveChangesAsync();
        }

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            seedDb.TenantConfigs.Add(TenantConfig.CreateWithConfig(
                tenantId,
                brandingJson: "{}",
                operationJson: JsonSerializer.Serialize(new Dictionary<string, object?> { ["sessionInactivityMinutes"] = 10 }),
                thresholdsJson: "{}",
                modulesJson: "{}",
                fiscalJson: "{}",
                printersJson: "[]",
                paymentsJson: "{}",
                maintenanceJson: "{}"));

            var user = AppUser.Create(tenantId, "Operador de teste", email: null, passwordHash: null, pinHash: "hash-pin-irrelevante");
            seedDb.Users.Add(user);

            var session = AuthSession.Create(
                tenantId, user.Id, deviceId: null, refreshHash: null, expiresAt: DateTimeOffset.UtcNow.AddHours(1));
            seedDb.AuthSessions.Add(session);
            sessionId = session.Id;

            await seedDb.SaveChangesAsync();

            // Timeout configurado é 10 minutos — 45 minutos de inatividade excede com folga.
            await seedDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE auth_session SET last_active_at = {DateTimeOffset.UtcNow.AddMinutes(-45)} WHERE id = {sessionId}");
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var guard = new AuthSessionActivityGuard(db);

        var result = await guard.EnforceAsync(tenantId, sessionId);

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.AuthSessionIdleTimeout);
    }

    [Fact]
    public async Task Sessao_Com_Atividade_Dentro_Do_Timeout_E_Aceita_E_Atualiza_LastActiveAt()
    {
        var tenantId = Guid.NewGuid();
        Guid sessionId;

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await seedDb.SaveChangesAsync();
        }

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            seedDb.TenantConfigs.Add(TenantConfig.CreateWithConfig(
                tenantId,
                brandingJson: "{}",
                operationJson: JsonSerializer.Serialize(new Dictionary<string, object?> { ["sessionInactivityMinutes"] = 30 }),
                thresholdsJson: "{}",
                modulesJson: "{}",
                fiscalJson: "{}",
                printersJson: "[]",
                paymentsJson: "{}",
                maintenanceJson: "{}"));

            var user = AppUser.Create(tenantId, "Operador de teste", email: null, passwordHash: null, pinHash: "hash-pin-irrelevante");
            seedDb.Users.Add(user);

            var session = AuthSession.Create(
                tenantId, user.Id, deviceId: null, refreshHash: null, expiresAt: DateTimeOffset.UtcNow.AddHours(1));
            seedDb.AuthSessions.Add(session);
            sessionId = session.Id;

            await seedDb.SaveChangesAsync();

            // 10 minutos de inatividade, dentro do timeout configurado de 30.
            await seedDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE auth_session SET last_active_at = {DateTimeOffset.UtcNow.AddMinutes(-10)} WHERE id = {sessionId}");
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var guard = new AuthSessionActivityGuard(db);
        var before = DateTimeOffset.UtcNow;

        var result = await guard.EnforceAsync(tenantId, sessionId);

        result.IsSuccess.Should().BeTrue();

        var reloadedSession = await db.AuthSessions.SingleAsync(s => s.Id == sessionId);
        reloadedSession.LastActiveAt.Should().BeOnOrAfter(before, "EnforceAsync deve renovar LastActiveAt a cada chamada bem-sucedida");
    }

    [Fact]
    public async Task Sessao_Revogada_E_Rejeitada_Mesmo_Dentro_Do_Timeout()
    {
        var tenantId = Guid.NewGuid();
        Guid sessionId;

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await seedDb.SaveChangesAsync();
        }

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var user = AppUser.Create(tenantId, "Operador de teste", email: null, passwordHash: null, pinHash: "hash-pin-irrelevante");
            seedDb.Users.Add(user);

            var session = AuthSession.Create(
                tenantId, user.Id, deviceId: null, refreshHash: null, expiresAt: DateTimeOffset.UtcNow.AddHours(1));
            session.Revoke();
            seedDb.AuthSessions.Add(session);
            sessionId = session.Id;

            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var guard = new AuthSessionActivityGuard(db);

        var result = await guard.EnforceAsync(tenantId, sessionId);

        result.IsFailure.Should().BeTrue();
        result.Code.Should().Be(ApiErrorCodes.AuthSessionIdleTimeout);
    }

    [Fact]
    public async Task Tenant_Sem_TenantConfig_Usa_O_Timeout_Default_De_30_Minutos()
    {
        var tenantId = Guid.NewGuid();
        Guid sessionId;

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
            await seedDb.SaveChangesAsync();
        }

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            // Deliberadamente SEM TenantConfig — SessionInactivityPolicy.DefaultMinutes (30) precisa
            // valer mesmo assim.
            var user = AppUser.Create(tenantId, "Operador de teste", email: null, passwordHash: null, pinHash: "hash-pin-irrelevante");
            seedDb.Users.Add(user);

            var session = AuthSession.Create(
                tenantId, user.Id, deviceId: null, refreshHash: null, expiresAt: DateTimeOffset.UtcNow.AddHours(1));
            seedDb.AuthSessions.Add(session);
            sessionId = session.Id;

            await seedDb.SaveChangesAsync();

            await seedDb.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE auth_session SET last_active_at = {DateTimeOffset.UtcNow.AddMinutes(-45)} WHERE id = {sessionId}");
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var guard = new AuthSessionActivityGuard(db);

        var result = await guard.EnforceAsync(tenantId, sessionId);

        result.IsFailure.Should().BeTrue("45 minutos de inatividade excede o default de 30 minutos");
        result.Code.Should().Be(ApiErrorCodes.AuthSessionIdleTimeout);
    }
}
