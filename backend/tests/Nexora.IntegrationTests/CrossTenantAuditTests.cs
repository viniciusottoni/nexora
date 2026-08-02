using Nexora.Application.Abstractions.Behaviors;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tenants.Commands.RecordCrossTenantAccessAttempt;
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
/// Cenário Gherkin "Tentativa de acesso cruzado por ID" da US-001 — verifica de ponta a ponta
/// (mesmo pipeline MediatR de produção: Validation -&gt; Logging -&gt; Transaction) que
/// <c>RecordCrossTenantAccessAttemptCommand</c> grava em <c>audit_log</c> sob RLS real, contra o
/// mesmo tenant do ator (nunca do alvo — o alvo nem precisa existir, só o ID é registrado).
/// </summary>
[Collection("Postgres")]
public sealed class CrossTenantAuditTests
{
    private readonly PostgresFixture _fixture;

    public CrossTenantAuditTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RecordCrossTenantAccessAttempt_Grava_Audit_Log_No_Tenant_Do_Ator()
    {
        var tenantA = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid(); // "outro tenant" nem precisa existir — só o ID é auditado
        var actorUserId = Guid.NewGuid();

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantA, $"tenant-a-{tenantA:N}", "Tenant A"));
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA));
        await using var provider = BuildMediatRContainer(db, new StaticTenantContext(tenantA));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(
            new RecordCrossTenantAccessAttemptCommand(tenantA, actorUserId, otherTenantId, "203.0.113.10"));

        result.IsSuccess.Should().BeTrue();

        var entries = await db.AuditLogs
            .Where(a => a.TenantId == tenantA && a.EntityId == otherTenantId)
            .ToListAsync();

        entries.Should().ContainSingle();
        var entry = entries[0];
        entry.Action.Should().Be("tenant.cross_tenant_access_attempt");
        entry.ActorId.Should().Be(actorUserId);
        entry.Ip.Should().Be("203.0.113.10");
    }

    private static ServiceProvider BuildMediatRContainer(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        // LoggingBehavior (ADR-022) passou a injetar ICurrentTenantContext para logar
        // tenantId/storeId/deviceId — sem este registro, a construção do behavior falha aqui.
        services.AddSingleton(tenantContext);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services.BuildServiceProvider();
    }
}
