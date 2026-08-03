using Nexora.Application.Audit.Commands.RecordAuditLogAccess;
using Nexora.Application.Audit.Queries.GetAuditLog;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// E-09/US-091 — filtros combinados, paginação por cursor e isolamento multi-tenant de
/// <c>GET /v1/audit</c> contra PostgreSQL real. O acesso negado por falta de permissão (cenário
/// "Acesso restrito e registrado", parte do 403) é responsabilidade da policy <c>AuditRead</c> em
/// <c>Program.cs</c> — mecanismo genérico já coberto por
/// <c>Nexora.UnitTests.Auth.PermissionAuthorizationTests</c> (mesmo predicado usado por toda
/// policy granular do produto); este arquivo cobre a QUERY em si.
/// </summary>
[Collection("Postgres")]
public sealed class AuditLogQueryIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AuditLogQueryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Filtro_Por_Autor_E_Periodo_Retorna_Somente_O_Esperado()
    {
        var tenantId = Guid.NewGuid();
        var operatorA = Guid.NewGuid();
        var operatorB = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await SeedTenantAsync(tenantId);
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            seedDb.AuditLogs.Add(AuditLog.Create(tenantId, "ORDER_CANCELLED", "order", now.AddDays(-10), actorId: operatorA));
            seedDb.AuditLogs.Add(AuditLog.Create(tenantId, "ORDER_CANCELLED", "order", now.AddHours(-1), actorId: operatorA));
            seedDb.AuditLogs.Add(AuditLog.Create(tenantId, "ORDER_CANCELLED", "order", now.AddHours(-1), actorId: operatorB));
            await seedDb.SaveChangesAsync();
        }

        var response = await SendQueryAsync(tenantId, new GetAuditLogQuery(
            From: now.AddDays(-1), To: now, ActorId: operatorA, AuthorizedBy: null,
            Action: null, Entity: null, EntityId: null, MinAmount: null, Limit: 50, Cursor: null));

        response.Data.Should().ContainSingle();
        response.Data[0].Actor!.Id.Should().Be(operatorA);
    }

    [Fact]
    public async Task Filtro_Por_Tipo_De_Acao_Retorna_Somente_Descontos()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await SeedTenantAsync(tenantId);

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            seedDb.AuditLogs.Add(AuditLog.Create(tenantId, "DISCOUNT_APPLIED", "table_session", now, after: """{"discount": 500}"""));
            seedDb.AuditLogs.Add(AuditLog.Create(tenantId, "ORDER_CANCELLED", "order", now));
            await seedDb.SaveChangesAsync();
        }

        var response = await SendQueryAsync(tenantId, new GetAuditLogQuery(
            null, null, null, null, Action: "DISCOUNT_APPLIED", Entity: null, EntityId: null, MinAmount: null, Limit: 50, Cursor: null));

        response.Data.Should().ContainSingle().Which.Action.Should().Be("DISCOUNT_APPLIED");
    }

    [Fact]
    public async Task Filtro_Por_Valor_Minimo_Considera_Apenas_Descontos_Acima_Do_Limite()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await SeedTenantAsync(tenantId);

        // Valores em reais (decimal), nunca centavos-inteiro — ADR-017 usa decimal de ponta a ponta
        // neste backend, sem convenção de centavos em nenhuma camada.
        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            seedDb.AuditLogs.Add(AuditLog.Create(tenantId, "DISCOUNT_APPLIED", "table_session", now, after: """{"discount": 10.00}""")); // R$ 10,00
            seedDb.AuditLogs.Add(AuditLog.Create(tenantId, "DISCOUNT_APPLIED", "table_session", now, after: """{"discount": 80.00}""")); // R$ 80,00
            await seedDb.SaveChangesAsync();
        }

        var response = await SendQueryAsync(tenantId, new GetAuditLogQuery(
            null, null, null, null, Action: null, Entity: null, EntityId: null, MinAmount: 50.00m, Limit: 50, Cursor: null));

        response.Data.Should().ContainSingle();
        response.Data[0].After!.Value.GetProperty("discount").GetDecimal().Should().Be(80.00m);
    }

    [Fact]
    public async Task Paginacao_Por_Cursor_Nao_Repete_Nem_Pula_Registro()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await SeedTenantAsync(tenantId);

        await using (var seedDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            for (var i = 0; i < 5; i++)
            {
                seedDb.AuditLogs.Add(AuditLog.Create(tenantId, "ORDER_CANCELLED", "order", now.AddMinutes(-i)));
            }
            await seedDb.SaveChangesAsync();
        }

        var firstPage = await SendQueryAsync(tenantId, new GetAuditLogQuery(
            null, null, null, null, null, null, null, null, Limit: 2, Cursor: null));

        firstPage.Data.Should().HaveCount(2);
        firstPage.Meta.HasMore.Should().BeTrue();
        firstPage.Meta.NextCursor.Should().NotBeNullOrEmpty();

        var secondPage = await SendQueryAsync(tenantId, new GetAuditLogQuery(
            null, null, null, null, null, null, null, null, Limit: 2, Cursor: firstPage.Meta.NextCursor));

        secondPage.Data.Should().HaveCount(2);
        secondPage.Data.Select(d => d.Id).Should().NotIntersectWith(firstPage.Data.Select(d => d.Id));
    }

    [Fact]
    public async Task Trilha_De_Um_Tenant_Nao_Aparece_Para_Outro()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await SeedTenantAsync(tenantA);
        await SeedTenantAsync(tenantB);

        await using (var seedDbA = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA)))
        {
            seedDbA.AuditLogs.Add(AuditLog.Create(tenantA, "ORDER_CANCELLED", "order", now));
            await seedDbA.SaveChangesAsync();
        }

        await using (var seedDbB = _fixture.CreateAppDbContext(new StaticTenantContext(tenantB)))
        {
            seedDbB.AuditLogs.Add(AuditLog.Create(tenantB, "ORDER_CANCELLED", "order", now));
            await seedDbB.SaveChangesAsync();
        }

        var response = await SendQueryAsync(tenantB, new GetAuditLogQuery(
            null, null, null, null, null, null, null, null, Limit: 50, Cursor: null));

        response.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordAuditLogAccess_Grava_Registro_De_Que_A_Trilha_Foi_Consultada()
    {
        var tenantId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        await SeedTenantAsync(tenantId);

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, userId: viewerId));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(new RecordAuditLogAccessCommand(new Dictionary<string, string?> { ["action"] = "DISCOUNT_APPLIED" }));

        result.IsSuccess.Should().BeTrue();
        var entry = await db.AuditLogs.SingleAsync(a => a.TenantId == tenantId && a.Action == "AUDIT_LOG_ACCESSED");
        entry.ActorId.Should().Be(viewerId);
    }

    private async Task SeedTenantAsync(Guid tenantId)
    {
        await using var seedDb = _fixture.CreateAppDbContext(tenantContext: null);
        seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant"));
        await seedDb.SaveChangesAsync();
    }

    private async Task<Nexora.Contracts.Audit.AuditLogListResponse> SendQueryAsync(Guid tenantId, GetAuditLogQuery query)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(query);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }
}
