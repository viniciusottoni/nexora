using Nexora.Application.Tenants.Commands.RecordSupportAccess;
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
/// E-09/US-090, cenário Gherkin "Acesso de suporte da plataforma" (EVT-074) — o ator (staff da
/// Replay, policy <c>PlatformAdmin</c>) não tem tenant próprio, então o <see cref="StaticTenantContext"/>
/// desta suíte é criado com <c>tenantContext: null</c> (mesmo estado de <c>CloudCurrentTenantContext.TenantId</c>
/// para um token sem claim <c>tid</c>) — o handler precisa fixar o tenant ALVO explicitamente via
/// <c>IApplicationDbContext.SetTenantContextAsync</c> para o INSERT passar pela política RLS
/// <c>tenant_isolation</c> (WITH CHECK). Usa <see cref="MediatRTestContainerFactory"/> (pipeline
/// completo de produção), não um container MediatR próprio.
/// </summary>
[Collection("Postgres")]
public sealed class SupportAccessAuditTests
{
    private readonly PostgresFixture _fixture;

    public SupportAccessAuditTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RecordSupportAccess_Grava_Audit_Log_E_Domain_Event_No_Tenant_Alvo()
    {
        var tenantId = Guid.NewGuid();
        var supportUserId = Guid.NewGuid();

        await using (var seedDb = _fixture.CreateAppDbContext(tenantContext: null))
        {
            seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant"));
            await seedDb.SaveChangesAsync();
        }

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(new RecordSupportAccessCommand(tenantId, supportUserId, "Investigação de chamado #4821", 30));

        result.IsSuccess.Should().BeTrue();

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.TenantId == tenantId && a.Action == "SUPPORT_ACCESS_GRANTED");
        auditEntry.ActorId.Should().Be(supportUserId);
        auditEntry.Reason.Should().Be("Investigação de chamado #4821");
        auditEntry.DomainEventId.Should().NotBeNull();

        var domainEvent = await db.DomainEvents.SingleAsync(e => e.Id == auditEntry.DomainEventId);
        domainEvent.Type.Should().Be("support.access.granted");
        domainEvent.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task RecordSupportAccess_Para_Tenant_Inexistente_Falha_Sem_Gravar_Nada()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(new RecordSupportAccessCommand(Guid.NewGuid(), Guid.NewGuid(), "Motivo qualquer", 15));

        result.IsFailure.Should().BeTrue();
    }
}
